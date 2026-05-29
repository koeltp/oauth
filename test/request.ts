import axios, { type AxiosRequestConfig, type AxiosResponse } from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import router from '@/router'
import { refreshToken as refreshTokenApi } from '@/api/auth'
import type { ApiResponse } from '@/types'

const axiosInstance = axios.create({
  baseURL: '/api',
  timeout: 10000
})

let isRefreshing = false
let pendingRequests: Array<{ resolve: (value: any) => void; reject: (reason?: any) => void }> = []

axiosInstance.interceptors.request.use(
  (config) => {
    const userStore = useUserStore()
    if (userStore.token) {
      config.headers.Authorization = `Bearer ${userStore.token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

axiosInstance.interceptors.response.use(
  (response: AxiosResponse<ApiResponse>) => {
    const res = response.data
    if (res.code !== 200) {
      if (res.code !== 401) {
        ElMessage.error(res.message || '请求失败')
      }
      if (res.code === 404) {
        const userStore = useUserStore()
        userStore.logout()
        router.push('/login')
      }
      return Promise.reject(new Error(res.message || '请求失败'))
    }
    return res
  },
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }

    if (error.response && error.response.status === 401 && !originalRequest._retry) {
      const userStore = useUserStore()

      if (!userStore.refreshTokenStr) {
        userStore.logout()
        router.push('/login')
        return Promise.reject(error)
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          pendingRequests.push({ resolve, reject })
        }).then(() => {
          originalRequest.headers = originalRequest.headers || {}
          originalRequest.headers.Authorization = `Bearer ${userStore.token}`
          return axiosInstance(originalRequest)
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const res: ApiResponse = await refreshTokenApi(userStore.refreshTokenStr)
        if (res.code === 200) {
          userStore.setToken(res.data.token)
          userStore.setRefreshToken(res.data.refreshToken)

          pendingRequests.forEach(({ resolve }) => resolve(undefined))
          pendingRequests = []

          originalRequest.headers = originalRequest.headers || {}
          originalRequest.headers.Authorization = `Bearer ${userStore.token}`
          return axiosInstance(originalRequest)
        } else {
          throw new Error(res.message || '刷新令牌失败')
        }
      } catch {
        userStore.logout()
        router.push('/login')
        pendingRequests.forEach(({ reject }) => reject(error))
        pendingRequests = []
        return Promise.reject(error)
      } finally {
        isRefreshing = false
      }
    }

    let errorMessage = error.message || '网络错误'

    if (error.response && error.response.status === 400) {
      const data = error.response.data
      if (data.errors) {
        const firstKey = Object.keys(data.errors)[0]
        if (firstKey && data.errors[firstKey] && data.errors[firstKey].length > 0) {
          errorMessage = data.errors[firstKey][0]
        } else if (data.title) {
          errorMessage = data.title
        }
      } else if (data.message) {
        errorMessage = data.message
      }
    } else if (error.response && error.response.data && error.response.data.message) {
      errorMessage = error.response.data.message
    }

    if (error.response && error.response.status === 403) {
      return Promise.resolve({ code: 403, message: '您没有权限执行此操作', data: null })
    }

    ElMessage.error(errorMessage)
    return Promise.reject(error)
  }
)

const request = <T = any>(config: AxiosRequestConfig): Promise<ApiResponse<T>> => {
  return axiosInstance(config)
}

export default request
