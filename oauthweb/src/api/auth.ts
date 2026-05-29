import request from '@/utils/api'
import type { UserInfo } from '@/types'

export interface LoginResponse {
  id: string
  username: string
  email: string
  access_token: string
  refresh_token: string
  expires_in: number
  refresh_expires_in: number
}

export interface RegisterRequest {
  email: string
  password: string
  username: string
}

export interface LoginRequest {
  email: string
  password: string
  twoFaCode?: string
}

export const loginWithPassword = (data: LoginRequest): Promise<LoginResponse> => {
  return request({
    url: '/auth/login/password',
    method: 'post',
    data
  })
}

export const sendEmailCode = (data: { email: string; purpose: number }): Promise<void> => {
  return request({
    url: '/auth/login/email-code',
    method: 'post',
    data
  })
}

export const sendSmsCode = (data: { phone: string; purpose: number }): Promise<void> => {
  return request({
    url: '/auth/login/sms-code',
    method: 'post',
    data
  })
}

export const verifyCode = (data: { email?: string; phone?: string; code: string; purpose: number }): Promise<void> => {
  return request({
    url: '/auth/verify-code',
    method: 'post',
    data
  })
}

export const register = (data: RegisterRequest): Promise<void> => {
  return request({
    url: '/auth/register',
    method: 'post',
    data
  })
}

export const getUserInfo = (): Promise<UserInfo> => {
  return request({
    url: '/auth/userinfo',
    method: 'get'
  })
}