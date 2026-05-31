import api from '@/utils/api'

export interface AdminLoginResponse {
  id: string
  username: string
  role: string
  avatarUrl: string | null
  accessToken: string
  refreshToken: string
  expiresIn: number
  refreshExpiresIn: number
}

export const loginWithPassword = (data: { username: string; password: string }): Promise<AdminLoginResponse> => {
  return api({
    url: '/admin/login',
    method: 'post',
    data
  })
}

export const sendEmailCode = (data: { email: string; purpose: number }): Promise<void> => {
  return api({
    url: '/auth/login/email-code',
    method: 'post',
    data
  })
}

export const sendSmsCode = (data: { phone: string; purpose: number }): Promise<void> => {
  return api({
    url: '/auth/login/sms-code',
    method: 'post',
    data
  })
}

export const verifyCode = (data: { email?: string; phone?: string; code: string; purpose: number }): Promise<void> => {
  return api({
    url: '/auth/verify-code',
    method: 'post',
    data
  })
}