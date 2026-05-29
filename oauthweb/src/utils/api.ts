import { createHttp } from './http'
import { useUserStore } from '@/stores/user'
import router from '@/router'
import { userRefreshTokenApi } from '@/api/refresh-token'

export const api = createHttp({
  baseURL: '/api/1.0',
  tokenGetter: () => useUserStore().token,
  refreshTokenGetter: () => useUserStore().refreshToken,
  tokenSetter: (token) => useUserStore().setToken(token),
  refreshTokenSetter: (token) => useUserStore().setRefreshToken(token),
  logoutHandler: () => {
    useUserStore().logout()
    router.push('/login')
  },
  refreshTokenApi: userRefreshTokenApi,
  loginPath: '/login'
})

export default api