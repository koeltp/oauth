import { ref, type Ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import { useAdminStore } from '@/stores/admin'

export function useLogin() {
  const router = useRouter()
  const adminStore = useAdminStore()
  const loading = ref(false)

  function handleLoginSuccess(res: any) {
    adminStore.setToken(res.accessToken)
    adminStore.setRefreshToken(res.refreshToken || '')
    adminStore.setAdminInfo({
      id: res.id,
      username: res.username,
      email: res.email || '',
      role: res.role,
      avatarUrl: res.avatarUrl || null,
      lastLoginAt: res.lastLoginAt
    })
    ElMessage.success('登录成功')
    router.push('/dashboard')
  }

  function startCountdown(countdown: Ref<number>) {
    countdown.value = 60
    const timer = setInterval(() => {
      countdown.value--
      if (countdown.value <= 0) clearInterval(timer)
    }, 1000)
  }

  return {
    loading,
    handleLoginSuccess,
    startCountdown
  }
}