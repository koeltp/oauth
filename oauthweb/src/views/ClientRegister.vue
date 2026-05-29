<template>
  <div class="client-register-container">
    <div class="register-box">
      <div class="register-header">
        <h1>注册 OAuth 客户端</h1>
        <p>创建一个新的应用程序以接入 OAuth 授权服务</p>
      </div>

      <el-steps :active="currentStep" finish-status="success" align-center class="register-steps">
        <el-step title="填写信息" />
        <el-step title="审核中" />
        <el-step title="完成" />
      </el-steps>

      <!-- 申请表单 -->
      <el-form 
        v-if="currentStep === 0"
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        class="register-form"
      >
        <el-form-item label="应用名称" prop="name">
          <el-input v-model="form.name" placeholder="输入应用名称" size="large" />
        </el-form-item>

        <el-form-item label="应用类型" prop="type">
          <el-select v-model="form.type" placeholder="选择应用类型" size="large" style="width: 100%">
            <el-option label="Web 应用" value="web" />
            <el-option label="移动应用" value="mobile" />
            <el-option label="桌面应用" value="desktop" />
            <el-option label="SPA 单页应用" value="spa" />
          </el-select>
        </el-form-item>

        <el-form-item label="应用描述" prop="description">
          <el-input 
            v-model="form.description" 
            type="textarea" 
            :rows="3"
            placeholder="简要描述您的应用功能"
          />
        </el-form-item>

        <el-form-item label="回调地址" prop="redirectUris">
          <el-input 
            v-model="form.redirectUris" 
            type="textarea"
            :rows="2"
            placeholder="输入回调地址，多个地址用换行分隔"
          />
          <div class="form-tip">用户授权后将被重定向到此处填写的 URL</div>
        </el-form-item>

        <el-form-item label="申请权限" prop="scopes">
          <el-checkbox-group v-model="form.scopes">
            <el-checkbox label="openid">OpenID Connect 身份验证</el-checkbox>
            <el-checkbox label="profile">获取用户基本信息</el-checkbox>
            <el-checkbox label="email">获取用户邮箱</el-checkbox>
            <el-checkbox label="phone">获取用户手机号</el-checkbox>
          </el-checkbox-group>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" size="large" style="width: 100%" @click="handleSubmit" :loading="loading">
            提交申请
          </el-button>
        </el-form-item>
      </el-form>

      <!-- 审核中状态 -->
      <div v-else-if="currentStep === 1" class="pending-status">
        <el-icon class="pending-icon" :size="64"><Clock /></el-icon>
        <h2>申请已提交</h2>
        <p>您的申请正在审核中，请耐心等待管理员审批</p>
        <p class="pending-tip">审核结果会通过邮件通知您</p>
        <el-button type="primary" @click="goToHome">返回首页</el-button>
      </div>

      <!-- 申请成功 -->
      <div v-else-if="currentStep === 2" class="success-status">
        <el-icon class="success-icon" :size="64"><SuccessFilled /></el-icon>
        <h2>申请已通过！</h2>
        <div class="client-info-card">
          <div class="info-row">
            <span class="label">Client ID:</span>
            <span class="value">{{ clientInfo.clientId }}</span>
            <el-button link @click="copyToClipboard(clientInfo.clientId)">复制</el-button>
          </div>
          <div class="info-row">
            <span class="label">Client Secret:</span>
            <span class="value secret">{{ clientInfo.clientSecret }}</span>
            <el-button link @click="copyToClipboard(clientInfo.clientSecret)">复制</el-button>
          </div>
        </div>
        <p class="warning-tip">请妥善保管 Client Secret，不要泄露给他人</p>
        <div class="action-buttons">
          <el-button type="primary" @click="goToAccount">查看我的应用</el-button>
          <el-button @click="goToHome">返回首页</el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Clock, SuccessFilled } from '@element-plus/icons-vue'
import api from '@/utils/api'

const router = useRouter()
const formRef = ref<FormInstance>()
const loading = ref(false)
const currentStep = ref(0)
const clientInfo = ref({
  clientId: '',
  clientSecret: ''
})

const form = reactive({
  name: '',
  type: '',
  description: '',
  redirectUris: '',
  scopes: ['openid'] as string[]
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入应用名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择应用类型', trigger: 'change' }],
  redirectUris: [
    { required: true, message: '请输入回调地址', trigger: 'blur' },
    { type: 'string', pattern: /^(https?:\/\/|\/)/, message: '回调地址必须以 http://、https:// 或 / 开头', trigger: 'blur' }
  ],
  scopes: [{ type: 'array', required: true, min: 1, message: '请至少选择一个权限', trigger: 'change' }]
}

const handleSubmit = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        const res: any = await api.post('/clients/register', {
          name: form.name,
          type: form.type,
          description: form.description,
          redirect_uris: form.redirectUris.split('\n').filter((uri: string) => uri.trim()),
          allowed_scopes: form.scopes
        })
        
        ElMessage.success('申请提交成功')
        
        if (res.status === 'Approved') {
          // 直接通过
          currentStep.value = 2
          clientInfo.value = {
            clientId: res.client_id,
            clientSecret: res.client_secret
          }
        } else {
          // 需要审核
          currentStep.value = 1
        }
      } catch (error: any) {
        ElMessage.error(error.response?.data?.message || '提交失败')
      } finally {
        loading.value = false
      }
    }
  })
}

const copyToClipboard = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text)
    ElMessage.success('已复制到剪贴板')
  } catch (error) {
    ElMessage.error('复制失败')
  }
}

const goToHome = () => {
  router.push('/')
}

const goToAccount = () => {
  router.push('/account')
}
</script>

<style scoped>
.client-register-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background: #f5f7fa;
  padding: 40px 20px;
}

.register-box {
  width: 500px;
  padding: 40px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
}

.register-header {
  text-align: center;
  margin-bottom: 30px;
}

.register-header h1 {
  font-size: 24px;
  color: #333;
  margin-bottom: 8px;
}

.register-header p {
  color: #666;
  font-size: 14px;
}

.register-steps {
  margin-bottom: 30px;
}

.register-form {
  margin-top: 20px;
}

.form-tip {
  font-size: 12px;
  color: #999;
  margin-top: 4px;
}

.pending-status,
.success-status {
  text-align: center;
  padding: 30px 0;
}

.pending-icon {
  color: #e6a23c;
  margin-bottom: 20px;
}

.success-icon {
  color: #67c23a;
  margin-bottom: 20px;
}

.pending-status h2,
.success-status h2 {
  font-size: 20px;
  color: #333;
  margin-bottom: 10px;
}

.pending-status p,
.success-status p {
  color: #666;
  margin-bottom: 8px;
}

.pending-tip {
  color: #999;
  font-size: 14px;
  margin-bottom: 20px;
}

.client-info-card {
  background: #f5f7fa;
  border-radius: 8px;
  padding: 20px;
  margin: 20px 0;
  text-align: left;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 0;
}

.info-row .label {
  font-weight: 600;
  color: #666;
  min-width: 100px;
}

.info-row .value {
  flex: 1;
  font-family: monospace;
  color: #333;
  word-break: break-all;
}

.info-row .value.secret {
  color: #e6a23c;
}

.warning-tip {
  color: #f56c6c;
  font-size: 14px;
  margin-bottom: 20px;
}

.action-buttons {
  display: flex;
  gap: 10px;
  justify-content: center;
}
</style>
