<template>
  <div class="verify-container">
    <div class="verify-box">
      <router-link to="/" class="home-icon" title="返回首页">
        <el-icon><HomeFilled /></el-icon>
      </router-link>
      <h1 class="title">邮箱验证</h1>

      <div v-if="verifying" class="status-section">
        <el-icon class="status-icon" :size="48" color="#409eff"><Loading /></el-icon>
        <p class="status-text">正在验证您的邮箱...</p>
      </div>

      <div v-if="verified" class="status-section">
        <el-icon class="status-icon" :size="48" color="#67c23a"><CircleCheck /></el-icon>
        <p class="status-text">邮箱验证成功</p>
        <el-button type="primary" size="large" style="width: 100%" @click="goToLogin">前往登录</el-button>
      </div>

      <div v-if="error" class="status-section">
        <el-icon class="status-icon" :size="48" color="#f56c6c"><CircleClose /></el-icon>
        <p class="status-text">{{ errorMsg }}</p>
        <el-button type="primary" size="large" style="width: 100%" @click="goToLogin">返回登录</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { CircleCheck, CircleClose, Loading, HomeFilled } from '@element-plus/icons-vue'
import request from '@/utils/api'

const route = useRoute()
const router = useRouter()

const verifying = ref(true)
const verified = ref(false)
const error = ref(false)
const errorMsg = ref('')

onMounted(async () => {
  const token = route.query.token as string
  if (!token) {
    verifying.value = false
    error.value = true
    errorMsg.value = '无效的验证链接'
    return
  }

  try {
    await request({
      url: '/auth/verify-email',
      method: 'post',
      data: { token }
    })
    verified.value = true
    ElMessage.success('邮箱验证成功')
  } catch (e: any) {
    error.value = true
    errorMsg.value = e.response?.data?.message || '验证失败，链接可能已过期'
  } finally {
    verifying.value = false
  }
})

function goToLogin() {
  router.push('/login')
}
</script>

<style scoped>
.verify-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
}

.home-icon {
  position: absolute;
  top: 20px;
  left: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  color: #999;
  text-decoration: none;
  border-radius: 8px;
  transition: all 0.3s;
}

.home-icon:hover {
  color: #409eff;
  background: #f5f7fa;
}

.verify-box {
  position: relative;
  width: 100%;
  max-width: 420px;
  margin: 0 auto;
  padding: 40px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
}

.title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 28px;
  font-weight: 600;
}

.status-section {
  text-align: center;
  padding: 20px 0;
}

.status-icon {
  margin-bottom: 16px;
}

.status-text {
  font-size: 16px;
  color: #333;
  margin-bottom: 24px;
}
</style>