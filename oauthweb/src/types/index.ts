// API 响应格式
export interface ApiResponse<T = any> {
  code: number
  message: string
  data: T
}

// 用户信息
export interface UserInfo {
  id: string
  username: string
  email: string
  two_factor_enabled: boolean
}

// 管理员信息
export interface AdminInfo {
  id: string
  username: string
  role: string
  avatarUrl: string | null
  lastLoginAt?: string
}

// 登录请求
export interface LoginRequest {
  username: string
  password: string
}

// 登录响应
export interface LoginResponse {
  access_token: string
  refresh_token?: string
  id: string
  username: string
  email?: string
  role?: string
  avatarUrl?: string
  two_factor_enabled?: boolean
  require_2fa?: boolean
  user_id?: string
}

// 刷新 token 请求
export interface RefreshTokenRequest {
  refreshToken: string
}

// 刷新 token 响应
export interface RefreshTokenResponse {
  access_token: string
  refresh_token: string
  token_type: string
  expires_in: number
}