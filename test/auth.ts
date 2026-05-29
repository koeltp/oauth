import request from '@/utils/request'
import type { LoginRequest } from '@/types'

export const login = (data: LoginRequest) => {
  return request({
    url: '/auth/login',
    method: 'post',
    data
  })
}

export const getUserInfo = () => {
  return request({
    url: '/auth/userinfo',
    method: 'get'
  })
}

export const refreshToken = (refreshToken: string) => {
  return request({
    url: '/auth/refresh',
    method: 'post',
    data: { refreshToken }
  })
}

export const logout = () => {
  return request({
    url: '/auth/logout',
    method: 'post'
  })
}
