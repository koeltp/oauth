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
  phone?: string
  emailVerified: boolean
  phoneVerified: boolean
  twoFactorEnabled: boolean
  status: string
  createdAt: string
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
  accessToken: string
  refreshToken?: string
  id: string
  username: string
  email?: string
  role?: string
  avatarUrl?: string
  twoFactorEnabled?: boolean
  require2Fa?: boolean
  userId?: string
}

// 刷新 token 请求
export interface RefreshTokenRequest {
  refreshToken: string
}

// 刷新 token 响应
export interface RefreshTokenResponse {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
}