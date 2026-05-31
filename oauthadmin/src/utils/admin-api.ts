import { createHttp } from './http'
import { useAdminStore } from '@/stores/admin'
import router from '@/router'
import { adminRefreshTokenApi } from '@/api/refresh-token'

export const adminApi = createHttp({
  baseURL: '/api/1.0',
  tokenGetter: () => useAdminStore().token,
  refreshTokenGetter: () => useAdminStore().refreshToken,
  tokenSetter: (token) => useAdminStore().setToken(token),
  refreshTokenSetter: (token) => useAdminStore().setRefreshToken(token),
  logoutHandler: () => {
    useAdminStore().logout()
    router.push('/admin/login')
  },
  refreshTokenApi: adminRefreshTokenApi,
  loginPath: '/admin/login'
})

export default adminApi