<template>
  <div class="authorize-container">
    <div class="authorize-box">
      <div v-if="loading" class="loading-state">
        <el-icon class="is-loading" :size="48"><Loading /></el-icon>
        <p>正在加载...</p>
      </div>

      <div v-else class="client-info">
        <div class="client-logo-wrapper">
          <el-icon :size="64" class="client-logo">
            <UserFilled />
          </el-icon>
        </div>
        <h2 class="client-name">{{ clientName || '未知应用' }}</h2>
        <p class="client-desc">{{ clientDescription || '正在请求访问您的账户' }}</p>
      </div>

      <el-card v-if="!loading" class="scope-list">
        <template #header>
          <span>将获取以下权限：</span>
        </template>
        <div 
          v-for="(scope, index) in displayScopes" 
          :key="index" 
          class="scope-item"
        >
          <el-icon><Check /></el-icon>
          <span>{{ scope.description }}</span>
        </div>
        <div v-if="displayScopes.length === 0" class="scope-item">
          <el-icon><User /></el-icon>
          <span>登录并验证您的身份</span>
        </div>
      </el-card>

      <div v-if="!loading" class="authorize-actions">
        <el-button type="primary" size="large" @click="handleAuthorize" :loading="authorizing">
          授权
        </el-button>
        <el-button size="large" @click="handleCancel">取消</el-button>
      </div>

      <p v-if="!loading && redirectUri" class="redirect-info">
        授权后将跳转到：<span class="redirect-uri">{{ displayRedirectUri }}</span>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Loading, Check, User, UserFilled } from '@element-plus/icons-vue'
import api from '@/utils/api'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const authorizing = ref(false)
const clientName = ref('')
const clientDescription = ref('')
const redirectUri = ref('')
const requestedScopes = ref<string[]>([])

const scopeDescriptions: Record<string, { name: string; description: string }> = {
  openid: { name: 'openid', description: 'OpenID Connect 身份验证' },
  profile: { name: 'profile', description: '获取您的基本信息（姓名、头像等）' },
  email: { name: 'email', description: '获取您的邮箱地址' },
  phone: { name: 'phone', description: '获取您的手机号码' }
}

const displayScopes = computed(() => {
  return requestedScopes.value
    .filter(scope => scopeDescriptions[scope])
    .map(scope => scopeDescriptions[scope])
})

const displayRedirectUri = computed(() => {
  try {
    const url = new URL(redirectUri.value)
    return url.origin + '/...'
  } catch {
    return redirectUri.value.length > 40 
      ? redirectUri.value.substring(0, 40) + '...' 
      : redirectUri.value
  }
})

onMounted(async () => {
  const clientId = route.query.client_id as string
  const redirectUriParam = route.query.redirect_uri as string
  const scopeParam = route.query.scope as string

  redirectUri.value = redirectUriParam || ''

  if (scopeParam) {
    requestedScopes.value = scopeParam.split(' ')
  }

  if (!clientId || !redirectUri.value) {
    ElMessage.error('缺少必要的参数')
    loading.value = false
    return
  }

  try {
    // 从后端获取客户端信息
    const res: any = await api.get(`/clients/${clientId}`)
    clientName.value = res.name
    clientDescription.value = res.description || ''
    
    // 如果客户端配置了 scopes，覆盖请求的 scopes
    if (res.allowedScopes && res.allowedScopes.length > 0) {
      requestedScopes.value = res.allowedScopes
    }
  } catch (error) {
    console.error('Failed to load client info:', error)
    // 如果获取失败，显示基本信息
    clientName.value = '应用 #' + clientId.substring(0, 8)
  } finally {
    loading.value = false
  }
})

const handleAuthorize = async () => {
  if (!redirectUri.value) {
    ElMessage.error('缺少回调地址')
    return
  }

  authorizing.value = true
  try {
    const params = new URLSearchParams({
      client_id: route.query.client_id as string,
      redirect_uri: route.query.redirect_uri as string,
      response_type: (route.query.response_type as string) || 'code',
      scope: requestedScopes.value.join(' '),
      state: (route.query.state as string) || ''
    })

    // 添加 PKCE 参数
    if (route.query.code_challenge) {
      params.set('code_challenge', route.query.code_challenge as string)
    }
    if (route.query.code_challenge_method) {
      params.set('code_challenge_method', route.query.code_challenge_method as string)
    }

    await api.post('/connect/authorize', Object.fromEntries(params), {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
    })

    ElMessage.success('授权成功')

    // 构建回调 URL
    const code = 'auth-code-' + Date.now() + '-' + Math.random().toString(36).substr(2)
    let redirectUrl = redirectUri.value
    redirectUrl += redirectUrl.includes('?') ? '&' : '?'
    redirectUrl += `code=${code}`
    
    if (route.query.state) {
      redirectUrl += `&state=${route.query.state}`
    }

    window.location.href = redirectUrl
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '授权失败')
    authorizing.value = false
  }
}

const handleCancel = () => {
  if (!redirectUri.value) {
    router.push('/')
    return
  }

  let redirectUrl = redirectUri.value
  redirectUrl += redirectUrl.includes('?') ? '&' : '?'
  redirectUrl += 'error=access_denied'
  
  if (route.query.state) {
    redirectUrl += `&state=${route.query.state}`
  }

  window.location.href = redirectUrl
}
</script>

<style scoped>
.authorize-container {
  width: 100%;
  height: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
  padding: 20px;
}

.authorize-box {
  width: 450px;
  padding: 40px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
  text-align: center;
}

.loading-state {
  padding: 60px 0;
}

.loading-state p {
  margin-top: 20px;
  color: #666;
}

.client-info {
  margin-bottom: 30px;
}

.client-logo-wrapper {
  width: 80px;
  height: 80px;
  margin: 0 auto 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.client-logo {
  color: white;
}

.client-name {
  font-size: 24px;
  color: #333;
  margin-bottom: 8px;
}

.client-desc {
  color: #666;
  font-size: 14px;
}

.scope-list {
  margin-bottom: 30px;
  text-align: left;
}

.scope-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 0;
  color: #333;
}

.scope-item:not(:last-child) {
  border-bottom: 1px solid #f0f0f0;
}

.scope-item .el-icon {
  color: #67c23a;
}

.authorize-actions {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
}

.authorize-actions .el-button {
  flex: 1;
}

.redirect-info {
  font-size: 12px;
  color: #999;
  word-break: break-all;
}

.redirect-uri {
  color: #409eff;
}
</style>
