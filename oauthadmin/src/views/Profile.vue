<template>
  <div class="profile-page">
    <!-- 主卡片容器 -->
    <div class="main-card">
      <!-- 页面标题 -->
      <div class="page-header">
        <h1>个人中心</h1>
        <p>管理您的个人信息和账户安全</p>
      </div>

      <!-- 分割线 -->
      <div class="divider"></div>

      <!-- 标签页 -->
      <div class="tabs">
        <div 
          v-for="tab in tabs" 
          :key="tab.key"
          :class="['tab', { active: activeTab === tab.key }]"
          @click="activeTab = tab.key"
        >
          {{ tab.label }}
        </div>
      </div>

      <!-- 分割线 -->
      <div class="divider"></div>

      <!-- 表单内容 -->
      <div class="form-section">
        <!-- 基本信息 -->
        <div v-if="activeTab === 'profile'" class="form-grid">
          <el-form ref="profileFormRef" :model="profileForm" :rules="profileRules" label-position="top">
            <!-- 头像上传 -->
            <div class="avatar-item">
              <div class="avatar-wrapper" @click="triggerAvatarUpload">
                <el-avatar :size="80" class="avatar" :src="profileForm.avatarUrl">
                  {{ profileForm.username?.charAt(0)?.toUpperCase() || 'A' }}
                </el-avatar>
              </div>
              <input 
                ref="avatarInput" 
                type="file" 
                accept="image/*" 
                class="avatar-input"
                @change="handleAvatarChange"
              />
            </div>

            <div class="form-item">
              <label>用户名</label>
              <el-input v-model="profileForm.username" disabled class="disabled-input" />
            </div>
            <div class="form-item">
              <label>邮箱</label>
              <el-form-item prop="email">
                <el-input v-model="profileForm.email" placeholder="请输入邮箱" />
              </el-form-item>
            </div>
            <div class="form-item">
              <label>角色</label>
              <span class="readonly-value">{{ profileForm.role }}</span>
            </div>
            <div class="form-item">
              <label>创建时间</label>
              <span class="readonly-value">{{ profileForm.createdAt }}</span>
            </div>
            <div class="form-item">
              <label>最后登录</label>
              <span class="readonly-value">{{ profileForm.lastLoginAt || '从未登录' }}</span>
            </div>
            <!-- 操作按钮 -->
            <div class="form-actions">
              <el-button type="primary" @click="handleSaveProfile" :loading="saving">保存修改</el-button>
            </div>
          </el-form>
        </div>

        <!-- 修改密码 -->
        <div v-if="activeTab === 'password'" class="form-grid">
          <el-form ref="formRef" :model="passwordForm" :rules="passwordRules" label-position="top">
            <div class="form-item">
              <label>当前密码</label>
              <el-form-item prop="currentPassword" class="form-item-content">
                <el-input v-model="passwordForm.currentPassword" type="password" show-password placeholder="请输入当前密码" />
              </el-form-item>
            </div>
            <div class="form-item">
              <label>新密码</label>
              <el-form-item prop="newPassword" class="form-item-content">
                <el-input v-model="passwordForm.newPassword" type="password" show-password placeholder="请输入新密码" />
              </el-form-item>
            </div>
            <div class="form-item">
              <label>确认密码</label>
              <el-form-item prop="confirmPassword" class="form-item-content">
                <el-input v-model="passwordForm.confirmPassword" type="password" show-password placeholder="请再次输入新密码" />
              </el-form-item>
            </div>
          </el-form>
          <!-- 操作按钮 -->
          <div class="form-actions">
            <el-button type="primary" @click="handleSubmitPassword" :loading="changingPassword">保存修改</el-button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getProfile, updateProfile, uploadAvatar, updatePassword } from '@/api/admin'
import type { FormInstance, FormRules } from 'element-plus'
import { useAdminStore } from '@/stores/admin'

const formRef = ref<FormInstance>()
const profileFormRef = ref<FormInstance>()

// Pinia Store
const adminStore = useAdminStore()

// 当前激活标签
const activeTab = ref('profile')

// 加载状态
const saving = ref(false)
const changingPassword = ref(false)

// 标签配置
const tabs = [
  { key: 'profile', label: '基本信息' },
  { key: 'password', label: '修改密码' }
]

// 基本信息表单 - 基于 Admin 实体
const profileForm = reactive({
  id: '',
  username: '',
  email: '',
  role: '',
  avatarUrl: '',
  createdAt: '',
  lastLoginAt: ''
})

// 密码表单
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// 基本信息表单验证规则
const profileRules: FormRules = {
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
   // { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ]
}

// 密码验证规则
const passwordRules: FormRules = {
  currentPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码长度不能少于8位', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value === passwordForm.currentPassword) {
          callback(new Error('新密码不能与当前密码相同'))
        } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(value)) {
          callback(new Error('密码需包含大小写字母和数字'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

// 头像输入框引用
const avatarInput = ref<HTMLInputElement | null>(null)

// 格式化时间
const formatDateTime = (dateString: string | null | undefined) => {
  if (!dateString) return '从未登录'
  try {
    const date = new Date(dateString)
    return date.toLocaleString('zh-CN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }).replace(/\//g, '-')
  } catch {
    return dateString
  }
}

// 获取用户信息
const fetchProfile = async () => {
  const res = await getProfile()
  profileForm.id = res.id
  profileForm.username = res.username
  profileForm.email = res.email || ''
  profileForm.role = res.role || '管理员'
  profileForm.avatarUrl = res.avatarUrl || ''
  profileForm.createdAt = formatDateTime(res.createdAt)
  profileForm.lastLoginAt = formatDateTime(res.lastLoginAt)
  
  if (adminStore.adminInfo) {
    adminStore.setAdminInfo({
      ...adminStore.adminInfo,
      avatarUrl: res.avatarUrl || ''
    })
  }
}

// 触发头像上传
const triggerAvatarUpload = () => {
  avatarInput.value?.click()
}

// 处理头像变化
const handleAvatarChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  if (file.size > 2 * 1024 * 1024) {
    ElMessage.error('图片大小不能超过2MB')
    return
  }

  if (!file.type.startsWith('image/')) {
    ElMessage.error('请选择图片文件')
    return
  }

  const formData = new FormData()
  formData.append('file', file)

  try {
    const res = await uploadAvatar(formData)
    profileForm.avatarUrl = res.avatarUrl
    adminStore.updateAvatar(res.avatarUrl)
    ElMessage.success(res.message || '头像上传成功')
  } finally {
    target.value = ''
  }
}

// 保存基本信息
const handleSaveProfile = async () => {
  if (!profileFormRef.value) return
  
  profileFormRef.value.validate(async (valid) => {
    if (!valid) return
    
    saving.value = true
    try {
      await updateProfile({ email: profileForm.email })
      ElMessage.success('保存成功')
      // 更新 Store 中的信息
      if (adminStore.adminInfo) {
        adminStore.setAdminInfo({ 
          ...adminStore.adminInfo, 
          email: profileForm.email 
        })
      }
    } finally {
      saving.value = false
    }
  })
}

// 提交密码修改
const handleSubmitPassword = async () => {
  if (!formRef.value) return
  
  formRef.value.validate(async (valid) => {
    if (!valid) return
    
    changingPassword.value = true
    try {
      await updatePassword({
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword
      })
      ElMessage.success('密码修改成功')
      // 重置表单和验证状态
      passwordForm.currentPassword = ''
      passwordForm.newPassword = ''
      passwordForm.confirmPassword = ''
      formRef.value?.resetFields()
      activeTab.value = 'profile'
    } finally {
      changingPassword.value = false
    }
  })
}

// 页面加载时获取用户信息
onMounted(() => {
  fetchProfile()
})
</script>

<style scoped>
.profile-page {
  width: 100%;
  min-height: 100%;
}

/* 主卡片容器 - 大气的全宽设计 */
.main-card {
  background: #fff;
  border-radius: 8px;
  min-height: calc(100vh - 100px);
}

/* 页面头部 */
.page-header {
  padding: 32px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  color: #303133;
  margin: 0 0 8px 0;
}

.page-header p {
  font-size: 14px;
  color: #909399;
  margin: 0;
}

/* 分割线 */
.divider {
  height: 1px;
  background: #e4e7ed;
}

/* 标签页 */
.tabs {
  display: flex;
  padding: 0 32px;
}

.tab {
  padding: 16px 24px;
  font-size: 14px;
  color: #606266;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
}

.tab:hover {
  color: #409eff;
}

.tab.active {
  color: #409eff;
  border-bottom-color: #409eff;
  font-weight: 500;
}

/* 表单区域 */
.form-section {
  padding: 32px;
}

/* 表单布局 */
.form-grid {
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.form-item {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 24px;
  padding: 12px 0;
}

.form-item label {
  font-size: 14px;
  font-weight: 500;
  color: #606266;
  min-width: 80px;
  margin-bottom: 0;
}

.form-item-content {
  flex: 1;
  margin-bottom: 0;
}

/* 头像上传项 */
.avatar-item {
  display: flex;
  flex-direction: column;
  margin-bottom: 20px;
}

.avatar-wrapper {
  cursor: pointer;
  width: fit-content;
}

.avatar {
  background: linear-gradient(135deg, #f5a623 0%, #f7b731 100%);
  font-size: 32px;
  font-weight: 600;
  color: #fff;
  transition: transform 0.2s, box-shadow 0.2s;
}

.avatar-wrapper:hover .avatar {
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.avatar-input {
  display: none;
}

.readonly-value {
  display: inline-block;
  padding: 8px 12px;
  background: #f5f7fa;
  border-radius: 4px;
  font-size: 14px;
  color: #606266;
}

.disabled-input {
  background: #f5f7fa;
  cursor: not-allowed;
}

/* 操作按钮 */
.form-actions {
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid #e4e7ed;
}

:deep(.el-button--primary) {
  background: #409eff;
  border: none;
  padding: 10px 24px;
  font-size: 14px;
}

:deep(.el-input__inner) {
  height: 40px;
  border-radius: 4px;
  border-color: #dcdfe6;
}

@media (max-width: 768px) {
  .page-header,
  .form-section {
    padding: 20px;
  }
  
  .tabs {
    padding: 0 20px;
  }
}
</style>
