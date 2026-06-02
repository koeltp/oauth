import { createHttp } from './http'
import { useAdminStore } from '@/stores/admin'
import router from '@/router'
import axios from 'axios'

export const adminRefreshTokenApi = (refreshToken: string) => {
  return axios.post('/api/1.0/admin/refresh', { refreshToken })
}

export const api = createHttp({
  baseURL: '/api/1.0',
  tokenGetter: () => useAdminStore().token,
  refreshTokenGetter: () => useAdminStore().refreshToken,
  tokenSetter: (token) => useAdminStore().setToken(token),
  refreshTokenSetter: (token) => useAdminStore().setRefreshToken(token),
  logoutHandler: () => {
    useAdminStore().logout()
    router.push('/login')
  },
  refreshTokenApi: adminRefreshTokenApi,
  loginPath: '/login'
})

export default api
