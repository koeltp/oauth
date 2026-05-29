<template>
  <div class="account-container">
    <div class="account-header">
      <h1>个人中心</h1>
      <el-button @click="handleLogout">退出登录</el-button>
    </div>

    <!-- 编辑资料弹窗 -->
    <el-dialog v-model="showEditDialog" title="编辑资料" width="400px" :close-on-click-modal="false">
      <el-form :model="editForm" ref="editFormRef" label-width="80px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="editForm.username" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="editForm.email" disabled />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="editForm.phone" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSaveProfile">保存</el-button>
      </template>
    </el-dialog>

    <!-- 2FA 二维码弹窗 -->
    <el-dialog v-model="show2FAModal" title="开启两步验证" width="400px" :close-on-click-modal="false">
      <div class="qr-container">
        <img :src="qrCodeUrl" alt="QR Code" class="qr-code" />
        <p class="qr-tip">请使用 Authenticator 应用扫描二维码</p>
        <div class="secret-info">
          <span class="secret-label">备用密钥：</span>
          <span class="secret-value">{{ twoFaSecret }}</span>
          <el-button link @click="copySecret">复制</el-button>
          <el-button link @click="downloadSecret">下载</el-button>
        </div>
      </div>
      <template #footer>
        <el-button @click="show2FAModal = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 禁用2FA弹窗 -->
    <el-dialog v-model="showDisable2FAModal" title="禁用两步验证" width="400px" :close-on-click-modal="false">
      <el-form :model="disable2FAForm" ref="disable2FAFormRef" label-width="80px">
        <el-form-item label="验证码" prop="code">
          <el-input v-model="disable2FAForm.code" placeholder="请输入两步验证码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDisable2FAModal = false">取消</el-button>
        <el-button type="danger" @click="confirmDisable2FA">确认禁用</el-button>
      </template>
    </el-dialog>

    <el-card class="account-card">
      <template #header>
        <div class="card-header">
          <span>基本信息</span>
          <el-button type="primary" link @click="openEditDialog">编辑</el-button>
        </div>
      </template>
      <div class="info-item">
        <span class="label">用户名：</span>
        <span class="value">{{ userInfo.username || '-' }}</span>
      </div>
      <div class="info-item">
        <span class="label">邮箱：</span>
        <span class="value">{{ userInfo.email || '-' }}</span>
        <el-tag v-if="userInfo.email_verified" type="success" size="small">已验证</el-tag>
      </div>
      <div class="info-item">
        <span class="label">手机：</span>
        <span class="value">{{ userInfo.phone || '-' }}</span>
        <el-tag v-if="userInfo.phone_verified" type="success" size="small">已验证</el-tag>
      </div>
      <div class="info-item">
        <span class="label">两步验证：</span>
        <el-tag :type="userInfo.two_factor_enabled ? 'success' : 'info'">
          {{ userInfo.two_factor_enabled ? '已开启' : '未开启' }}
        </el-tag>
        <el-button v-if="!userInfo.two_factor_enabled" type="primary" link size="small" @click="handleEnable2FA">
          立即开启
        </el-button>
        <el-button v-else type="danger" link size="small" @click="handleDisable2FA">
          禁用
        </el-button>
      </div>
    </el-card>

    <el-card class="account-card">
      <template #header>
        <div class="card-header">
          <span>开发者工具</span>
        </div>
      </template>
      <div class="dev-tools">
        <div class="tool-item" @click="goToClientRegister">
          <el-icon class="tool-icon"><Key /></el-icon>
          <div class="tool-info">
            <span class="tool-name">注册 OAuth 客户端</span>
            <span class="tool-desc">创建新的应用程序接入 OAuth 授权服务</span>
          </div>
          <el-icon class="tool-arrow"><ArrowRight /></el-icon>
        </div>
      </div>
    </el-card>

    <el-card class="account-card">
      <template #header>
        <div class="card-header">
          <span>已授权的应用</span>
        </div>
      </template>
      <el-table :data="authorizedApps" v-loading="loading">
        <el-table-column prop="name" label="应用名称" />
        <el-table-column prop="scope" label="授权范围" />
        <el-table-column prop="created_at" label="授权时间" />
        <el-table-column label="操作">
          <template #default="{ row }">
            <el-button type="danger" link size="small" @click="handleRevoke(row)">撤销</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card class="account-card">
      <template #header>
        <div class="card-header">
          <span>第三方账号绑定</span>
        </div>
      </template>
      <div class="bind-list">
        <div class="bind-item">
          <el-icon><Link /></el-icon>
          <span>GitHub</span>
          <el-tag v-if="boundProviders.includes('Github')" type="success">已绑定</el-tag>
          <el-button v-else type="primary" link @click="handleBindGithub">绑定</el-button>
        </div>
        <div class="bind-item">
          <el-icon><ChatDotRound /></el-icon>
          <span>微信</span>
          <el-tag v-if="boundProviders.includes('Wechat')" type="success">已绑定</el-tag>
          <el-button v-else type="primary" link @click="handleBindWechat">绑定</el-button>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Link, ChatDotRound, Key, ArrowRight } from '@element-plus/icons-vue'
import api from '@/utils/api'

const router = useRouter()
const userInfo = ref<any>({})
const authorizedApps = ref<any[]>([])
const boundProviders = ref<string[]>([])
const loading = ref(false)

// 2FA 相关
const show2FAModal = ref(false)
const qrCodeUrl = ref('')
const twoFaSecret = ref('')

// 禁用2FA相关
const showDisable2FAModal = ref(false)
const disable2FAFormRef = ref()
const disable2FAForm = ref({
  code: ''
})

// 编辑资料相关
const showEditDialog = ref(false)
const editFormRef = ref()
const editForm = ref({
  username: '',
  email: '',
  phone: ''
})

const openEditDialog = () => {
  editForm.value = {
    username: userInfo.value.username || '',
    email: userInfo.value.email || '',
    phone: userInfo.value.phone || ''
  }
  showEditDialog.value = true
}

const handleSaveProfile = async () => {
  try {
    await api.put('/auth/profile', {
      username: editForm.value.username,
      phone: editForm.value.phone
    })
    ElMessage.success('资料更新成功')
    showEditDialog.value = false
    // 更新本地用户信息
    userInfo.value.username = editForm.value.username
    userInfo.value.phone = editForm.value.phone
    localStorage.setItem('user_info', JSON.stringify(userInfo.value))
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '更新失败')
  }
}

onMounted(async () => {
  await loadUserInfo()
  await loadAuthorizedApps()
  await loadBoundProviders()
})

const loadUserInfo = async () => {
  try {
    const userStr = localStorage.getItem('user_info')
    if (userStr) {
      userInfo.value = JSON.parse(userStr)
    }
  } catch (error) {
    console.error('Failed to load user info')
  }
}

const loadAuthorizedApps = async () => {
  loading.value = true
  try {
    const res = await api.get('/user/authorizations')
    authorizedApps.value = res.data || []
  } catch (error) {
    console.error('Failed to load authorized apps')
  } finally {
    loading.value = false
  }
}

const loadBoundProviders = async () => {
  try {
    const res = await api.get('/user/external/accounts')
    boundProviders.value = (res.data || []).map((a: any) => a.provider)
  } catch (error) {
    console.error('Failed to load bound providers')
  }
}

const handleLogout = () => {
  localStorage.removeItem('user_token')
  localStorage.removeItem('user_refresh_token')
  localStorage.removeItem('user_info')
  router.push('/login')
}

const handleEnable2FA = async () => {
  try {
    await ElMessageBox.confirm('确定要开启两步验证吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    const res: any = await api.post('/auth/2fa/enable')
    // 生成二维码 URL（使用 Google Charts API）
    const encodedUrl = encodeURIComponent(res.qr_code_url)
    qrCodeUrl.value = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodedUrl}`
    twoFaSecret.value = res.secret
    show2FAModal.value = true
    // 更新用户信息
    userInfo.value.two_factor_enabled = true
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '操作失败')
    }
  }
}

const copySecret = async () => {
  try {
    await navigator.clipboard.writeText(twoFaSecret.value)
    ElMessage.success('已复制到剪贴板')
  } catch (error) {
    ElMessage.error('复制失败')
  }
}

const downloadSecret = () => {
  try {
    const content = `OAuth 两步验证备用密钥\n\n用户: ${userInfo.value.email}\n密钥: ${twoFaSecret.value}\n\n请妥善保管此密钥，用于在无法使用 Authenticator 应用时恢复账户访问。`
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `2fa-backup-key-${userInfo.value.username || 'user'}.txt`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
    ElMessage.success('密钥已下载')
  } catch (error) {
    ElMessage.error('下载失败')
  }
}

const handleDisable2FA = () => {
  showDisable2FAModal.value = true
}

const confirmDisable2FA = async () => {
  try {
    await api.post('/auth/2fa/disable', {
      code: disable2FAForm.value.code
    })
    ElMessage.success('两步验证已禁用')
    showDisable2FAModal.value = false
    userInfo.value.two_factor_enabled = false
    localStorage.setItem('user_info', JSON.stringify(userInfo.value))
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '操作失败')
  }
}

const handleRevoke = async (app: any) => {
  try {
    await ElMessageBox.confirm('确定要撤销该应用的授权吗？', '警告', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await api.delete(`/user/authorizations/${app.id}`)
    ElMessage.success('已撤销授权')
    await loadAuthorizedApps()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '操作失败')
    }
  }
}

const handleBindGithub = () => {
  window.location.href = '/api/external/github/authorize'
}

const handleBindWechat = () => {
  window.location.href = '/api/external/wechat/authorize'
}

const goToClientRegister = () => {
  router.push('/client/register')
}
</script>

<style scoped>
.account-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 30px;
}

.account-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
}

.account-header h1 {
  font-size: 24px;
  color: #333;
}

.account-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.info-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 0;
  border-bottom: 1px solid #eee;
}

.info-item:last-child {
  border-bottom: none;
}

.info-item .label {
  color: #666;
  width: 100px;
}

.info-item .value {
  color: #333;
}

.bind-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.bind-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  border: 1px solid #eee;
  border-radius: 8px;
}

.bind-item span {
  flex: 1;
}

.dev-tools {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.tool-item {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 15px;
  border: 1px solid #eee;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
}

.tool-item:hover {
  border-color: #409eff;
  background-color: #f5f7fa;
}

.tool-icon {
  font-size: 24px;
  color: #409eff;
}

.tool-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.tool-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
}

.tool-desc {
  font-size: 12px;
  color: #999;
}

.tool-arrow {
  color: #ccc;
}

.qr-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 20px;
}

.qr-code {
  width: 200px;
  height: 200px;
  border-radius: 8px;
}

.qr-tip {
  margin-top: 15px;
  color: #666;
  font-size: 14px;
}

.secret-info {
  margin-top: 15px;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 15px;
  background: #f5f5f5;
  border-radius: 8px;
}

.secret-label {
  color: #666;
  font-size: 14px;
}

.secret-value {
  font-family: monospace;
  font-size: 14px;
  color: #333;
  word-break: break-all;
}
</style>
