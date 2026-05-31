import axios from 'axios'

export const adminRefreshTokenApi = (refreshToken: string): Promise<{ accessToken: string; refreshToken: string }> => {
  return axios.post('/api/1.0/admin/refresh', { refreshToken })
}