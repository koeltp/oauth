import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface AdminInfo {
  id: string
  username: string
  email: string
  role: string
  avatarUrl: string | null
  lastLoginAt?: string
}

export const useAdminStore = defineStore('admin', () => {
  // 状态 - 使用 localStorage 持久化登录状态
  const token = ref<string | null>(localStorage.getItem('admin_token'))
  const refreshToken = ref<string | null>(localStorage.getItem('admin_refresh_token'))
  const adminInfo = ref<AdminInfo | null>(null)
  
  // 从 localStorage 初始化 adminInfo
  const savedAdminInfo = localStorage.getItem('admin_info')
  if (savedAdminInfo) {
    try {
      adminInfo.value = JSON.parse(savedAdminInfo)
    } catch {
      adminInfo.value = null
    }
  }

  // 计算属性
  const isLoggedIn = computed(() => !!token.value)
  
  const avatarText = computed(() => {
    if (!adminInfo.value) return 'A'
    return adminInfo.value.avatarUrl ? null : adminInfo.value.username.charAt(0).toUpperCase()
  })

  // 操作
  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('admin_token', newToken)
  }

  function setRefreshToken(newRefreshToken: string) {
    refreshToken.value = newRefreshToken
    localStorage.setItem('admin_refresh_token', newRefreshToken)
  }

  function setAdminInfo(info: AdminInfo) {
    adminInfo.value = info
    localStorage.setItem('admin_info', JSON.stringify(info))
  }

  function updateAvatar(avatarUrl: string) {
    if (adminInfo.value) {
      adminInfo.value.avatarUrl = avatarUrl
      localStorage.setItem('admin_info', JSON.stringify(adminInfo.value))
    }
  }

  function logout() {
    token.value = null
    refreshToken.value = null
    adminInfo.value = null
    localStorage.removeItem('admin_token')
    localStorage.removeItem('admin_refresh_token')
    localStorage.removeItem('admin_info')
  }

  return {
    token,
    refreshToken,
    adminInfo,
    isLoggedIn,
    avatarText,
    setToken,
    setRefreshToken,
    setAdminInfo,
    updateAvatar,
    logout
  }
})