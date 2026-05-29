import adminApi from '@/utils/admin-api'

export interface AdminLoginResponse {
  id: string
  username: string
  email: string
  role: string
  avatarUrl: string | null
  access_token: string
  refresh_token: string
  expires_in: number
  refresh_expires_in: number
}

export interface AdminProfileResponse {
  id: string
  username: string
  email: string | null
  avatarUrl: string | null
  role: string
  createdAt: string
  lastLoginAt: string | null
}

export interface AdminLoginRequest {
  username: string
  password: string
}

export interface UpdateProfileRequest {
  username?: string
  email?: string
}

export interface UpdatePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AvatarUploadResponse {
  message: string
  avatarUrl: string
}

export const login = (data: AdminLoginRequest): Promise<AdminLoginResponse> => {
  return adminApi({
    url: '/admin/login',
    method: 'post',
    data
  })
}

export const getProfile = (): Promise<AdminProfileResponse> => {
  return adminApi({
    url: '/admin/profile',
    method: 'get'
  })
}

export const updateProfile = (data: UpdateProfileRequest): Promise<void> => {
  return adminApi({
    url: '/admin/profile',
    method: 'put',
    data
  })
}

export const uploadAvatar = (data: FormData): Promise<AvatarUploadResponse> => {
  return adminApi({
    url: '/admin/profile/avatar',
    method: 'post',
    data
  })
}

export const updatePassword = (data: UpdatePasswordRequest): Promise<void> => {
  return adminApi({
    url: '/admin/profile/password',
    method: 'put',
    data
  })
}