<template>
  <div class="forgot-container">
    <div class="forgot-box">
      <router-link to="/" class="home-icon" title="返回首页">
        <el-icon><HomeFilled /></el-icon>
      </router-link>
      <h1 class="title">忘记密码</h1>

      <template v-if="step === 'send'">
        <p class="hint">请输入您的注册邮箱，我们将发送验证码</p>
        <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
          <el-form-item prop="email">
            <el-input v-model="form.email" placeholder="注册邮箱" size="large" :prefix-icon="Message" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" size="large" style="width: 100%" :loading="sending" @click="handleSendCode">
              发送验证码
            </el-button>
          </el-form-item>
        </el-form>
      </template>

      <template v-if="step === 'reset'">
        <p class="hint">验证码已发送至 {{ form.email }}，请查收</p>
        <el-form ref="resetFormRef" :model="resetForm" :rules="resetRules" label-position="top">
          <el-form-item prop="code">
            <el-input v-model="resetForm.code" placeholder="验证码" size="large" :prefix-icon="Key">
              <template #suffix>
                <el-button link type="primary" :disabled="countdown > 0" @click="handleResendCode">
                  {{ countdown > 0 ? `${countdown}s` : '重新发送' }}
                </el-button>
              </template>
            </el-input>
          </el-form-item>
          <el-form-item prop="newPassword">
            <el-input v-model="resetForm.newPassword" type="password" placeholder="新密码" size="large" :prefix-icon="Lock" show-password />
          </el-form-item>
          <el-form-item prop="confirmPassword">
            <el-input v-model="resetForm.confirmPassword" type="password" placeholder="确认新密码" size="large" :prefix-icon="Lock" show-password />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" size="large" style="width: 100%" :loading="resetting" @click="handleResetPassword">
              重置密码
            </el-button>
          </el-form-item>
        </el-form>
      </template>

      <template v-if="step === 'done'">
        <div class="done-section">
          <el-icon class="done-icon" :size="48" color="#67c23a"><CircleCheck /></el-icon>
          <p class="done-text">密码重置成功</p>
          <el-button type="primary" size="large" style="width: 100%" @click="goToLogin">返回登录</el-button>
        </div>
      </template>

      <div class="back-link" v-if="step !== 'send' && step !== 'done'">
        <router-link to="/login">返回登录</router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Message, Lock, Key, CircleCheck, HomeFilled } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import request from '@/utils/api'

const router = useRouter()

const step = ref<'send' | 'reset' | 'done'>('send')
const sending = ref(false)
const resetting = ref(false)
const countdown = ref(0)

const formRef = ref<FormInstance>()
const resetFormRef = ref<FormInstance>()

const form = reactive({ email: '' })
const rules: FormRules = {
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ]
}

const resetForm = reactive({
  code: '',
  newPassword: '',
  confirmPassword: ''
})

const validateConfirm = (_rule: any, value: string, callback: any) => {
  if (value !== resetForm.newPassword) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const resetRules: FormRules = {
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码长度不能少于8位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirm, trigger: 'blur' }
  ]
}

function startCountdown() {
  countdown.value = 60
  const timer = setInterval(() => {
    countdown.value--
    if (countdown.value <= 0) clearInterval(timer)
  }, 1000)
}

async function handleSendCode() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    sending.value = true
    try {
      await request({
        url: '/auth/forgot-password',
        method: 'post',
        data: { email: form.email }
      })
      ElMessage.success('验证码已发送')
      startCountdown()
      step.value = 'reset'
    } catch {
    } finally {
      sending.value = false
    }
  })
}

async function handleResendCode() {
  sending.value = true
  try {
    await request({
      url: '/auth/forgot-password',
      method: 'post',
      data: { email: form.email }
    })
    ElMessage.success('验证码已重新发送')
    startCountdown()
  } catch {
  } finally {
    sending.value = false
  }
}

async function handleResetPassword() {
  if (!resetFormRef.value) return
  await resetFormRef.value.validate(async (valid) => {
    if (!valid) return
    resetting.value = true
    try {
      await request({
        url: '/auth/reset-password',
        method: 'post',
        data: {
          email: form.email,
          code: resetForm.code,
          newPassword: resetForm.newPassword
        }
      })
      ElMessage.success('密码重置成功')
      step.value = 'done'
    } catch {
    } finally {
      resetting.value = false
    }
  })
}

function goToLogin() {
  router.push('/login')
}
</script>

<style scoped>
.forgot-container {
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

.forgot-box {
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
  margin-bottom: 12px;
  color: #333;
  font-size: 28px;
  font-weight: 600;
}

.hint {
  text-align: center;
  color: #999;
  font-size: 14px;
  margin-bottom: 24px;
}

.back-link {
  text-align: center;
  margin-top: 20px;
}

.back-link a {
  color: #999;
  text-decoration: none;
  font-size: 13px;
}

.back-link a:hover {
  color: #409eff;
  text-decoration: underline;
}

.done-section {
  text-align: center;
  padding: 20px 0;
}

.done-icon {
  margin-bottom: 16px;
}

.done-text {
  font-size: 18px;
  color: #333;
  margin-bottom: 24px;
}
</style>