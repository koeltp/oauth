<template>
  <div class="github-callback">
    <div class="loading-card">
      <el-icon class="loading-icon" :size="48" v-if="!error && !isBinding">
        <Loading />
      </el-icon>
      <el-icon class="loading-icon" :size="48" v-else-if="isBinding">
        <Loading />
      </el-icon>
      <el-icon class="error-icon" :size="48" v-else color="#f56c6c">
        <WarningFilled />
      </el-icon>
      <h2>{{ title }}</h2>
      <p>{{ message }}</p>
      <el-button v-if="error && !isBinding" type="primary" @click="goBack" style="margin-top: 16px;">
        {{ backButtonText }}
      </el-button>
      <el-button v-else-if="boundSuccess" type="primary" @click="goToDashboard" style="margin-top: 16px;">
        返回控制台
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading, WarningFilled } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import api from '@/utils/api'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const error = ref('')
const isBinding = ref(false)
const boundSuccess = ref(false)

const title = computed(() => {
  if (boundSuccess.value) return '绑定成功'
  if (isBinding.value) return '正在绑定...'
  return error.value ? '失败' : 'GitHub 登录中...'
})

const message = computed(() => {
  if (boundSuccess.value) return 'GitHub 账号已成功绑定到当前用户'
  if (isBinding.value) return '正在完成账号绑定，请稍候...'
  return error.value || '正在完成身份验证，请稍候...'
})

const backButtonText = computed(() => {
  return error.value?.includes('已被其他用户绑定') ? '返回控制台' : '返回登录'
})

onMounted(async () => {
  const accessToken = route.query.access_token as string
  const userId = route.query.user_id as string
  const username = route.query.username as string
  const email = route.query.email as string
  const errorMsg = route.query.error as string
  const providerUserId = route.query.provider_user_id as string
  const redirectUrl = route.query.redirect_url as string || ''

  if (errorMsg) {
    error.value = decodeURIComponent(errorMsg)
    ElMessage.error(error.value)
    return
  }

  // 绑定模式：收到 provider_user_id，调用绑定 API
  if (providerUserId) {
    isBinding.value = true
    try {
      await api.post('/external/bind', {
        provider: 'Github',
        providerUserId
      })
      boundSuccess.value = true
      ElMessage.success('GitHub 账号绑定成功')
    } catch (err: any) {
      error.value = err.response?.data?.message || '绑定失败'
      ElMessage.error(error.value)
    } finally {
      isBinding.value = false
    }
    return
  }

  // 登录模式
  if (!accessToken || !userId) {
    error.value = '登录参数不完整，请重试'
    ElMessage.error(error.value)
    return
  }

  userStore.setToken(accessToken)
  userStore.setUserInfo({
    id: userId,
    username,
    email: email || '',
    phone: '',
    emailVerified: !!email,
    phoneVerified: false,
    twoFactorEnabled: false,
    status: 'Active',
    createdAt: ''
  })

  ElMessage.success('GitHub 登录成功')

  if (redirectUrl) {
    router.push(decodeURIComponent(redirectUrl))
  } else {
    router.push('/dashboard')
  }
})

function goBack() {
  if (error.value?.includes('已被其他用户绑定')) {
    router.push('/dashboard')
  } else {
    router.push('/login')
  }
}

function goToDashboard() {
  router.push('/dashboard')
}
</script>

<style scoped>
.github-callback {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: #f5f7fa;
}

.loading-card {
  text-align: center;
  padding: 48px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
}

.loading-icon {
  animation: spin 1s linear infinite;
  color: #409eff;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.error-icon {
  margin-bottom: 8px;
}

h2 {
  margin: 16px 0 8px;
  color: #333;
}

p {
  color: #666;
  margin: 0;
}
</style>