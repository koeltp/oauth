import { ref, type Ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'

export function useLogin() {
  const router = useRouter()
  const userStore = useUserStore()
  const loading = ref(false)

  function handleLoginSuccess(res: any) {
    userStore.setToken(res.accessToken)
    userStore.setRefreshToken(res.refreshToken || '')
    userStore.setUserInfo({
      id: res.userId,
      username: res.username,
      email: res.email,
      phone: res.phone || '',
      emailVerified: res.emailVerified || false,
      phoneVerified: res.phoneVerified || false,
      twoFactorEnabled: res.twoFactorEnabled || false,
      status: res.status || 'Active',
      createdAt: res.createdAt || ''
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
    startCountdown,
    router,
    userStore
  }
}