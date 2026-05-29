import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { UserInfo } from '@/types'

export const useUserStore = defineStore('user', () => {
  // 状态 - 使用 localStorage 持久化登录状态
  const token = ref<string | null>(localStorage.getItem('user_token'))
  const refreshToken = ref<string | null>(localStorage.getItem('user_refresh_token'))
  const userInfo = ref<UserInfo | null>(null)
  
  // 从 localStorage 初始化 userInfo
  const savedUserInfo = localStorage.getItem('user_info')
  if (savedUserInfo) {
    try {
      userInfo.value = JSON.parse(savedUserInfo)
    } catch {
      userInfo.value = null
    }
  }

  // 计算属性
  const isLoggedIn = computed(() => !!token.value)

  // 操作
  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('user_token', newToken)
  }

  function setRefreshToken(newRefreshToken: string) {
    refreshToken.value = newRefreshToken
    localStorage.setItem('user_refresh_token', newRefreshToken)
  }

  function setUserInfo(info: UserInfo) {
    userInfo.value = info
    localStorage.setItem('user_info', JSON.stringify(info))
  }

  function logout() {
    token.value = null
    refreshToken.value = null
    userInfo.value = null
    localStorage.removeItem('user_token')
    localStorage.removeItem('user_refresh_token')
    localStorage.removeItem('user_info')
  }

  return {
    token,
    refreshToken,
    userInfo,
    isLoggedIn,
    setToken,
    setRefreshToken,
    setUserInfo,
    logout
  }
})