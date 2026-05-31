import { createHttp } from './http'
import { useAdminStore } from '@/stores/admin'
import router from '@/router'

export const adminRefreshTokenApi = (refreshToken: string) => {
  return fetch('/api/1.0/admin/refresh-token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  }).then(res => res.json())
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
