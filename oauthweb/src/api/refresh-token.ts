import axios from 'axios'

export const userRefreshTokenApi = (refreshToken: string): Promise<{ access_token: string; refresh_token: string }> => {
  return axios.post('/api/1.0/auth/refresh', { refreshToken })
}

export const adminRefreshTokenApi = (refreshToken: string): Promise<{ access_token: string; refresh_token: string }> => {
  return axios.post('/api/1.0/admin/refresh', { refreshToken })
}