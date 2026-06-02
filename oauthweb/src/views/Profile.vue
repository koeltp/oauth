<template>
  <UserLayout>
    <div class="profile-container">
      <el-card class="profile-card">
        <template #header>
          <div class="card-header">
            <div class="tabs">
              <span :class="['tab', { active: activeTab === 'profile' }]" @click="activeTab = 'profile'">个人信息</span>
              <span :class="['tab', { active: activeTab === 'security' }]" @click="activeTab = 'security'">安全设置</span>
              <span :class="['tab', { active: activeTab === 'password' }]" @click="activeTab = 'password'">修改密码</span>
            </div>
          </div>
        </template>

        <!-- 个人信息 -->
        <el-form v-if="activeTab === 'profile'" ref="profileFormRef" :model="profileForm" :rules="profileRules" label-width="100px" class="profile-form">
          <el-form-item label="用户名" prop="username">
            <el-input v-model="profileForm.username" />
          </el-form-item>
          <el-form-item label="邮箱">
            <el-input :model-value="userInfo.email" disabled />
          </el-form-item>
          <el-form-item label="手机号">
            <div class="phone-row">
              <span class="phone-value">{{ userInfo.phone || '未绑定' }}</span>
              <el-tag v-if="userInfo.phoneVerified" type="success" size="small">已验证</el-tag>
              <el-button v-if="!userInfo.phone" type="primary" link size="small" @click="handleBindPhone">绑定</el-button>
              <el-button v-else-if="!userInfo.phoneVerified" type="warning" link size="small" @click="handleBindPhone">去验证</el-button>
              <el-button v-else type="primary" link size="small" @click="handleBindPhone">换绑</el-button>
            </div>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="saving" @click="handleSaveProfile">保存修改</el-button>
          </el-form-item>
        </el-form>

        <!-- 安全设置 -->
        <div v-if="activeTab === 'security'" class="security-section">
          <div class="security-item">
            <div class="security-left">
              <span class="security-label">两步验证</span>
              <span class="security-desc">启用两步验证以增强账户安全性</span>
            </div>
            <div class="security-right">
              <el-tag :type="userInfo.twoFactorEnabled ? 'success' : 'info'">
                {{ userInfo.twoFactorEnabled ? '已开启' : '未开启' }}
              </el-tag>
              <el-button v-if="!userInfo.twoFactorEnabled" type="primary" size="small" @click="handleEnable2FA">
                立即开启
              </el-button>
              <el-button v-else type="danger" size="small" @click="handleDisable2FA">
                禁用
              </el-button>
            </div>
          </div>
        </div>

        <!-- 修改密码 -->
        <el-form v-if="activeTab === 'password'" ref="passwordFormRef" :model="passwordForm" :rules="passwordRules" label-width="100px" class="profile-form">
          <el-form-item label="原密码" prop="oldPassword">
            <el-input v-model="passwordForm.oldPassword" type="password" show-password />
          </el-form-item>
          <el-form-item label="新密码" prop="newPassword">
            <el-input v-model="passwordForm.newPassword" type="password" show-password />
          </el-form-item>
          <el-form-item label="确认密码" prop="confirmPassword">
            <el-input v-model="passwordForm.confirmPassword" type="password" show-password />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="changingPassword" @click="handleChangePassword">修改密码</el-button>
          </el-form-item>
        </el-form>
      </el-card>
    </div>

    <!-- 2FA 二维码弹窗 -->
    <el-dialog v-model="show2FAModal" title="开启两步验证" width="450px" :close-on-click-modal="false" class="dialog-with-spacing">
      <div class="qr-container">
        <div class="qr-code-wrapper">
          <img :src="qrCodeUrl" alt="QR Code" class="qr-code" />
        </div>
        <p class="qr-tip">请使用 Authenticator 应用扫描二维码</p>
        <div class="secret-info">
          <span class="secret-label">备用密钥：</span>
          <div class="secret-value-row">
            <span class="secret-value">{{ twoFaSecret }}</span>
            <el-button link @click="copySecret">
              <el-icon><CopyDocument /></el-icon>复制
            </el-button>
            <el-button link @click="downloadSecret">
              <el-icon><Download /></el-icon>下载
            </el-button>
          </div>
        </div>
        <el-divider />
        <el-form :model="confirm2FAForm" ref="confirm2FAFormRef" label-width="80px" class="full-width-form">
          <el-form-item label="验证码" prop="code" :rules="[{ required: true, message: '请输入验证码' }]">
            <el-input-otp v-model="confirm2FAForm.code" :length="6" placeholder="请输入验证码" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="show2FAModal = false">取消</el-button>
        <el-button type="primary" :loading="confirming2FA" @click="handleConfirm2FA">确认开启</el-button>
      </template>
    </el-dialog>

    <!-- 禁用2FA弹窗 -->
    <el-dialog v-model="showDisable2FAModal" title="禁用两步验证" width="400px" :close-on-click-modal="false" class="dialog-with-spacing">
      <el-form :model="disable2FAForm" label-width="80px">
        <el-form-item label="验证码" prop="code">
          <el-input-otp v-model="disable2FAForm.code" :length="6" placeholder="请输入验证码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDisable2FAModal = false">取消</el-button>
        <el-button type="danger" @click="confirmDisable2FA">确认禁用</el-button>
      </template>
    </el-dialog>

    <!-- 绑定手机弹窗 -->
    <el-dialog v-model="showBindPhoneDialog" title="绑定手机" width="420px" :close-on-click-modal="false">
      <el-form :model="bindPhoneForm" ref="bindPhoneFormRef" label-width="80px">
        <el-form-item label="手机号" prop="phone" :rules="[
          { required: true, message: '请输入手机号' },
          { pattern: /^1[3-9]\d{9}$/, message: '请输入有效的手机号' }
        ]">
          <el-input v-model="bindPhoneForm.phone" placeholder="请输入手机号" maxlength="11" />
        </el-form-item>
        <el-form-item label="验证码" prop="code" :rules="[{ required: true, message: '请输入验证码' }]">
          <div class="code-input-wrapper">
            <el-input v-model="bindPhoneForm.code" placeholder="输入验证码" maxlength="6" />
            <el-button :disabled="sendCodeCountdown > 0" @click="handleSendBindCode">
              {{ sendCodeCountdown > 0 ? `${sendCodeCountdown}s` : '发送验证码' }}
            </el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showBindPhoneDialog = false">取消</el-button>
        <el-button type="primary" :loading="bindingPhone" @click="handleConfirmBindPhone">确认绑定</el-button>
      </template>
    </el-dialog>
  </UserLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { CopyDocument, Download } from '@element-plus/icons-vue'
import UserLayout from '@/layouts/UserLayout.vue'
import api from '@/utils/api'

const userInfo = ref<any>(JSON.parse(localStorage.getItem('user_info') || '{}'))
const activeTab = ref('profile')

// ========== 个人信息 ==========
const profileFormRef = ref<FormInstance>()
const profileForm = ref({
  username: userInfo.value.username || ''
})
const profileRules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }]
}
const saving = ref(false)

const handleSaveProfile = async () => {
  if (!profileFormRef.value) return
  try {
    await profileFormRef.value.validate()
  } catch {
    return
  }
  saving.value = true
  try {
    await api.put('/auth/profile', { username: profileForm.value.username })
    ElMessage.success('资料更新成功')
    userInfo.value.username = profileForm.value.username
    localStorage.setItem('user_info', JSON.stringify(userInfo.value))
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '更新失败')
  } finally {
    saving.value = false
  }
}

// ========== 安全设置 ==========
// 2FA 相关
const show2FAModal = ref(false)
const qrCodeUrl = ref('')
const twoFaSecret = ref('')
const confirm2FAFormRef = ref()
const confirm2FAForm = ref({ code: '' })
const confirming2FA = ref(false)

const showDisable2FAModal = ref(false)
const disable2FAForm = ref({ code: '' })

const handleEnable2FA = async () => {
  try {
    await ElMessageBox.confirm('确定要开启两步验证吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    const res: any = await api.post('/auth/2fa/enable')
    const encodedUrl = encodeURIComponent(res.qrCodeUrl)
    qrCodeUrl.value = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodedUrl}`
    twoFaSecret.value = res.secret
    confirm2FAForm.value.code = ''
    show2FAModal.value = true
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
  } catch {
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
  } catch {
    ElMessage.error('下载失败')
  }
}

const handleDisable2FA = () => {
  showDisable2FAModal.value = true
}

const handleConfirm2FA = async () => {
  if (!confirm2FAFormRef.value) return
  try {
    await confirm2FAFormRef.value.validate()
  } catch {
    return
  }
  confirming2FA.value = true
  try {
    await api.post('/auth/2fa/confirm', { code: confirm2FAForm.value.code })
    ElMessage.success('两步验证已开启')
    show2FAModal.value = false
    userInfo.value.twoFactorEnabled = true
    localStorage.setItem('user_info', JSON.stringify(userInfo.value))
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '验证失败')
  } finally {
    confirming2FA.value = false
  }
}

const confirmDisable2FA = async () => {
  if (!disable2FAForm.value.code) {
    ElMessage.warning('请输入验证码')
    return
  }
  try {
    await api.post('/auth/2fa/disable', { code: disable2FAForm.value.code })
    ElMessage.success('两步验证已禁用')
    showDisable2FAModal.value = false
    disable2FAForm.value.code = ''
    userInfo.value.twoFactorEnabled = false
    localStorage.setItem('user_info', JSON.stringify(userInfo.value))
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '禁用失败')
  }
}

// ========== 修改密码 ==========
const passwordFormRef = ref<FormInstance>()
const passwordForm = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const passwordRules: FormRules = {
  oldPassword: [{ required: true, message: '请输入原密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于 6 位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule: any, value: string, callback: any) => {
        if (value !== passwordForm.value.newPassword) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}
const changingPassword = ref(false)

const handleChangePassword = async () => {
  if (!passwordFormRef.value) return
  try {
    await passwordFormRef.value.validate()
  } catch {
    return
  }
  changingPassword.value = true
  try {
    await api.put('/auth/change-password', {
      oldPassword: passwordForm.value.oldPassword,
      newPassword: passwordForm.value.newPassword
    })
    ElMessage.success('密码修改成功')
    passwordForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '修改失败')
  } finally {
    changingPassword.value = false
  }
}

// ========== 绑定手机 ==========
const showBindPhoneDialog = ref(false)
const bindPhoneFormRef = ref()
const bindPhoneForm = ref({ phone: '', code: '' })
const bindingPhone = ref(false)
const sendCodeCountdown = ref(0)

const handleBindPhone = () => {
  bindPhoneForm.value = { phone: '', code: '' }
  sendCodeCountdown.value = 0
  showBindPhoneDialog.value = true
}

const handleSendBindCode = async () => {
  if (!/^1[3-9]\d{9}$/.test(bindPhoneForm.value.phone)) {
    ElMessage.error('请输入有效的手机号')
    return
  }
  try {
    await api.post('/auth/login/sms-code', { phone: bindPhoneForm.value.phone, purpose: 3 })
    ElMessage.success('验证码已发送')
    sendCodeCountdown.value = 60
    const timer = setInterval(() => {
      sendCodeCountdown.value--
      if (sendCodeCountdown.value <= 0) clearInterval(timer)
    }, 1000)
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '发送失败')
  }
}

const handleConfirmBindPhone = async () => {
  if (!bindPhoneFormRef.value) return
  try {
    await bindPhoneFormRef.value.validate()
  } catch {
    return
  }
  bindingPhone.value = true
  try {
    await api.post('/auth/bind-phone', { phone: bindPhoneForm.value.phone, code: bindPhoneForm.value.code })
    ElMessage.success('手机绑定成功')
    showBindPhoneDialog.value = false
    userInfo.value.phone = bindPhoneForm.value.phone
    userInfo.value.phoneVerified = true
    localStorage.setItem('user_info', JSON.stringify(userInfo.value))
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '绑定失败')
  } finally {
    bindingPhone.value = false
  }
}
</script>

<style scoped>
.profile-container {
  max-width: 1200px;
  margin: 0 auto;
}

.profile-card {
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.tabs {
  display: flex;
  gap: 0;
}

.tab {
  padding: 12px 24px;
  font-size: 14px;
  font-weight: 500;
  color: #909399;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
  margin-bottom: -18px;
}

.tab:hover {
  color: #409eff;
}

.tab.active {
  color: #409eff;
  border-bottom-color: #409eff;
}

.profile-form {
  padding: 20px 0;
}

.phone-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.phone-value {
  color: #333;
}

.code-input-wrapper {
  display: flex;
  gap: 8px;
}

.code-input-wrapper .el-input {
  flex: 1;
}

.security-section {
  padding: 10px 0;
}

.security-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 0;
  border-bottom: 1px solid #f0f0f0;
}

.security-item:last-child {
  border-bottom: none;
}

.security-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.security-label {
  font-size: 14px;
  font-weight: 500;
  color: #303133;
}

.security-desc {
  font-size: 12px;
  color: #909399;
}

.security-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.qr-container {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 12px;
  width: 100%;
}

.qr-code-wrapper {
  display: flex;
  justify-content: center;
  width: 100%;
}

.qr-code {
  width: 180px;
  height: 180px;
  border: 1px solid #e8e8e8;
  border-radius: 12px;
  padding: 12px;
  background: #fff;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
}

.qr-tip {
  font-size: 13px;
  color: #909399;
  text-align: center;
  margin: 0;
  align-self: center;
}

.secret-info {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  font-size: 13px;
  width: 100%;
}

.secret-label {
  color: #666;
  white-space: nowrap;
  width: 80px;
  text-align: right;
  flex-shrink: 0;
}

.secret-value-row {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
  justify-content: flex-end;
}

.secret-value {
  font-family: monospace;
  color: #333;
  word-break: break-all;
  font-size: 12px;
}

:deep(.full-width-form) {
  width: 100%;
}

:deep(.full-width-form .el-form-item) {
  margin-bottom: 0;
  width: 100%;
  display: flex;
  align-items: center;
}

:deep(.full-width-form .el-form-item__label-wrap) {
  flex-shrink: 0;
  width: 80px;
}

:deep(.full-width-form .el-form-item__content) {
  flex: 1;
  margin-left: 0 !important;
}

:deep(.full-width-form .el-form-item__content .el-input) {
  width: 100%;
}

:deep(.dialog-with-spacing .el-dialog__footer) {
  padding-top: 24px;
}
</style>
