import axios, { type AxiosRequestConfig } from 'axios'
import { ElMessage } from 'element-plus'

interface HttpConfig {
  baseURL: string
  tokenGetter: () => string | null
  refreshTokenGetter: () => string | null
  tokenSetter: (token: string) => void
  refreshTokenSetter: (token: string) => void
  logoutHandler: () => void
  refreshTokenApi: (token: string) => Promise<any>
  loginPath: string
}

interface HttpInstance {
  get: <T = any>(url: string, config?: AxiosRequestConfig) => Promise<T>
  post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => Promise<T>
  put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig) => Promise<T>
  delete: <T = any>(url: string, config?: AxiosRequestConfig) => Promise<T>
  <T = any>(config: AxiosRequestConfig): Promise<T>
}

export function createHttp(config: HttpConfig) {
  const axiosInstance = axios.create({
    baseURL: config.baseURL,
    timeout: 30000
  })

  let isRefreshing = false
  let pendingRequests: Array<{ resolve: (value: any) => void; reject: (reason?: any) => void }> = []

  axiosInstance.interceptors.request.use(
    (requestConfig) => {
      const token = config.tokenGetter()
      if (token) {
        requestConfig.headers = requestConfig.headers || {} as any
        requestConfig.headers.Authorization = `Bearer ${token}`
      }
      return requestConfig
    },
    (error) => Promise.reject(error)
  )

  const handleTokenRefresh = async (originalRequest: any): Promise<any> => {
    if (originalRequest._retry) {
      config.logoutHandler()
      return Promise.reject(new Error('登录已过期'))
    }

    originalRequest._retry = true
    const refreshToken = config.refreshTokenGetter()

    if (!refreshToken) {
      config.logoutHandler()
      return Promise.reject(new Error('登录已过期'))
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        pendingRequests.push({ resolve, reject })
      }).then(() => {
        originalRequest.headers = originalRequest.headers || {}
        originalRequest.headers.Authorization = `Bearer ${config.tokenGetter()}`
        return axiosInstance(originalRequest)
      })
    }

    isRefreshing = true

    try {
      const res = await config.refreshTokenApi(refreshToken)
      config.tokenSetter(res.accessToken)
      config.refreshTokenSetter(res.refreshToken)

      pendingRequests.forEach(({ resolve }) => resolve(undefined))
      pendingRequests = []

      originalRequest.headers = originalRequest.headers || {}
      originalRequest.headers.Authorization = `Bearer ${config.tokenGetter()}`
      return axiosInstance(originalRequest)
    } catch {
      config.logoutHandler()
      pendingRequests.forEach(({ reject }) => reject(new Error('登录已过期')))
      pendingRequests = []
      return Promise.reject(new Error('登录已过期'))
    } finally {
      isRefreshing = false
    }
  }

  axiosInstance.interceptors.response.use(
    (response) => {
      const body = response.data

      if (!body || typeof body !== 'object' || !('code' in body)) {
        return body
      }

      const { code, message } = body

      if (code === 401) {
        return handleTokenRefresh(response.config)
      }

      if (code !== 200) {
        ElMessage.error(message || '请求失败')
        return Promise.reject(new Error(message))
      }

      return body.data ?? body
    },
    async (error) => {
      if (!error.response) {
        ElMessage.error('网络连接失败，请检查网络')
        return Promise.reject(error)
      }

      ElMessage.error(error.response?.data?.message || '网络错误')
      return Promise.reject(error)
    }
  )

  const http = ((requestConfig: AxiosRequestConfig) => axiosInstance(requestConfig)) as HttpInstance
  http.get = (url, config) => axiosInstance.get(url, config)
  http.post = (url, data, config) => axiosInstance.post(url, data, config)
  http.put = (url, data, config) => axiosInstance.put(url, data, config)
  http.delete = (url, config) => axiosInstance.delete(url, config)

  return http
}