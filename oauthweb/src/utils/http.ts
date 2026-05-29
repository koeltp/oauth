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

  axiosInstance.interceptors.response.use(
    (response) => {
      const data = response.data
      // 如果是统一响应格式，提取 data 字段
      if (data && typeof data === 'object' && 'code' in data && 'data' in data) {
        return data.data
      }
      return data
    },
    async (error) => {
      const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }
      
      if (error.response?.status === 401 && !originalRequest._retry) {
        const refreshToken = config.refreshTokenGetter()
        
        if (!refreshToken) {
          config.logoutHandler()
          return Promise.reject(error)
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

        originalRequest._retry = true
        isRefreshing = true

        try {
          const res = await config.refreshTokenApi(refreshToken)
          config.tokenSetter(res.access_token)
          config.refreshTokenSetter(res.refresh_token)
          
          pendingRequests.forEach(({ resolve }) => resolve(undefined))
          pendingRequests = []

          originalRequest.headers = originalRequest.headers || {}
          originalRequest.headers.Authorization = `Bearer ${config.tokenGetter()}`
          return axiosInstance(originalRequest)
        } catch {
          config.logoutHandler()
          pendingRequests.forEach(({ reject }) => reject(error))
          pendingRequests = []
          return Promise.reject(error)
        } finally {
          isRefreshing = false
        }
      }

      const errorMessage = error.response?.data?.message || error.message || '网络错误'
      ElMessage.error(errorMessage)
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