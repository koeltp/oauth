import axios from 'axios'

export const userRefreshTokenApi = (refreshToken: string): Promise<{ accessToken: string; refreshToken: string }> => {
  return axios.post('/api/1.0/auth/refresh', { refreshToken })
}

export const adminRefreshTokenApi = (refreshToken: string): Promise<{ accessToken: string; refreshToken: string }> => {
  return axios.post('/api/1.0/admin/refresh', { refreshToken })
}