<template>
  <div class="github-callback">
    <div class="loading-card">
      <el-icon class="loading-icon" :size="48" v-if="!error">
        <Loading />
      </el-icon>
      <el-icon class="error-icon" :size="48" v-else color="#f56c6c">
        <WarningFilled />
      </el-icon>
      <h2>{{ error ? '登录失败' : 'GitHub 登录中...' }}</h2>
      <p>{{ error || '正在完成身份验证，请稍候...' }}</p>
      <el-button v-if="error" type="primary" @click="goToLogin" style="margin-top: 16px;">
        返回登录
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading, WarningFilled } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const error = ref('')

onMounted(async () => {
  const accessToken = route.query.access_token as string
  const userId = route.query.user_id as string
  const username = route.query.username as string
  const email = route.query.email as string
  const errorMsg = route.query.error as string

  if (errorMsg) {
    error.value = decodeURIComponent(errorMsg)
    ElMessage.error(error.value)
    return
  }

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
  router.push('/dashboard')
})

function goToLogin() {
  router.push('/login')
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