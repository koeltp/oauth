<template>
  <div class="login-container">
    <div class="login-box">
      <router-link to="/" class="home-icon" title="返回首页">
        <el-icon><HomeFilled /></el-icon>
      </router-link>
      <h1 class="title">OAuth Server</h1>
      
      <el-tabs v-model="activeTab" class="login-tabs">
        <!-- 密码登录 -->
        <el-tab-pane label="密码登录" name="password">
          <el-form ref="passwordFormRef" :model="passwordForm" :rules="passwordRules" label-position="top">
            <el-form-item prop="email">
              <el-input v-model="passwordForm.email" placeholder="邮箱" size="large" :prefix-icon="Message" />
            </el-form-item>
            <el-form-item prop="password">
              <el-input v-model="passwordForm.password" type="password" placeholder="密码" size="large" :prefix-icon="Lock" show-password />
            </el-form-item>
            <el-form-item v-if="showTwoFaField" prop="twoFaCode">
              <el-input v-model="passwordForm.twoFaCode" placeholder="两步验证码" size="large" :prefix-icon="Key" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" size="large" style="width: 100%" @click="handlePasswordLogin" :loading="loading">
                登录
              </el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 邮箱验证码登录 -->
        <el-tab-pane label="邮箱验证码" name="email">
          <el-form ref="emailFormRef" :model="emailForm" :rules="emailRules" label-position="top">
            <el-form-item prop="email">
              <el-input v-model="emailForm.email" placeholder="邮箱" size="large" :prefix-icon="Message" />
            </el-form-item>
            <el-form-item prop="code">
              <el-input v-model="emailForm.code" placeholder="验证码" size="large" :prefix-icon="Key">
                <template #append>
                  <el-button @click="handleSendEmailCode" :disabled="emailCountdown > 0">
                    {{ emailCountdown > 0 ? `${emailCountdown}s` : '发送验证码' }}
                  </el-button>
                </template>
              </el-input>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" size="large" style="width: 100%" @click="handleEmailLogin" :loading="loading">
                登录
              </el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 短信验证码登录 -->
        <el-tab-pane label="短信验证码" name="sms">
          <el-form ref="smsFormRef" :model="smsForm" :rules="smsRules" label-position="top">
            <el-form-item prop="phone">
              <el-input v-model="smsForm.phone" placeholder="手机号" size="large" :prefix-icon="Phone" />
            </el-form-item>
            <el-form-item prop="code">
              <el-input v-model="smsForm.code" placeholder="验证码" size="large" :prefix-icon="Key">
                <template #append>
                  <el-button @click="handleSendSmsCode" :disabled="smsCountdown > 0">
                    {{ smsCountdown > 0 ? `${smsCountdown}s` : '发送验证码' }}
                  </el-button>
                </template>
              </el-input>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" size="large" style="width: 100%" @click="handleSmsLogin" :loading="loading">
                登录
              </el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>
      </el-tabs>

      <!-- 第三方登录 -->
      <div class="external-login">
        <el-divider>其他登录方式</el-divider>
        <div class="external-buttons">
          <el-button @click="handleGithubLogin" :icon="Link" circle size="large" />
          <el-button @click="handleWechatLogin" :icon="ChatDotRound" circle size="large" />
        </div>
      </div>

      <div class="register-link">
        还没有账号？ <router-link to="/register">立即注册</router-link>
      </div>

      <div class="admin-link">
        <router-link to="/admin/login">管理员登录</router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Message, Lock, Key, Phone, Link, ChatDotRound, HomeFilled } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { loginWithPassword, sendEmailCode, sendSmsCode, verifyCode } from '@/api/auth'

const router = useRouter()
const userStore = useUserStore()

const activeTab = ref('password')
const loading = ref(false)
const emailCountdown = ref(0)
const smsCountdown = ref(0)

// 2FA 相关
const showTwoFaField = ref(false)
const currentUserId = ref('')

// 密码登录
const passwordFormRef = ref<FormInstance>()
const passwordForm = reactive({
  email: '',
  password: '',
  twoFaCode: ''
})
const passwordRules: FormRules = {
  email: [{ required: true, message: '请输入邮箱', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const handlePasswordLogin = async () => {
      if (!passwordFormRef.value) return
      await passwordFormRef.value.validate(async (valid) => {
        if (valid) {
          loading.value = true
          try {
            const res: any = await loginWithPassword(passwordForm)
            if (res.require_2fa) {
              // 需要 2FA 验证，显示 2FA 输入框
              showTwoFaField.value = true
              currentUserId.value = res.user_id
            } else {
              ElMessage.success('登录成功')
              userStore.setToken(res.access_token)
              userStore.setRefreshToken(res.refresh_token || '')
              userStore.setUserInfo({
                id: res.user_id,
                username: res.username,
                email: res.email,
                two_factor_enabled: res.two_factor_enabled || false
              })
              router.push('/account')
            }
          } catch {
          } finally {
            loading.value = false
          }
        }
      })
    }

// 邮箱验证码登录
const emailFormRef = ref<FormInstance>()
const emailForm = reactive({
  email: '',
  code: ''
})
const emailRules: FormRules = {
  email: [{ required: true, message: '请输入邮箱', trigger: 'blur' }],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }]
}

const handleSendEmailCode = async () => {
  if (!emailForm.email) {
    ElMessage.warning('请输入邮箱')
    return
  }
  try {
    await sendEmailCode({
      email: emailForm.email,
      purpose: 0 // Login
    })
    ElMessage.success('验证码已发送')
    emailCountdown.value = 60
    const timer = setInterval(() => {
      emailCountdown.value--
      if (emailCountdown.value <= 0) clearInterval(timer)
    }, 1000)
  } catch (error) {
    ElMessage.error('发送失败')
  }
}

const handleEmailLogin = async () => {
      if (!emailFormRef.value) return
      await emailFormRef.value.validate(async (valid) => {
        if (valid) {
          loading.value = true
          try {
            const res: any = await verifyCode({
              email: emailForm.email,
              code: emailForm.code,
              purpose: 0
            })
            if (res.require_2fa) {
              ElMessage.warning('请使用密码登录并输入两步验证码')
            } else if (res.verified) {
              ElMessage.success('登录成功')
              userStore.setToken(res.access_token)
              userStore.setRefreshToken(res.refresh_token || '')
              userStore.setUserInfo({
                id: res.user_id,
                username: res.username,
                email: res.email,
                two_factor_enabled: res.two_factor_enabled || false
              })
              router.push('/account')
            }
          } catch {
          } finally {
            loading.value = false
          }
        }
      })
    }

// 短信验证码登录
const smsFormRef = ref<FormInstance>()
const smsForm = reactive({
  phone: '',
  code: ''
})
const smsRules: FormRules = {
  phone: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }]
}

const handleSendSmsCode = async () => {
  if (!smsForm.phone) {
    ElMessage.warning('请输入手机号')
    return
  }
  try {
    await sendSmsCode({
      phone: smsForm.phone,
      purpose: 0
    })
    ElMessage.success('验证码已发送')
    smsCountdown.value = 60
    const timer = setInterval(() => {
      smsCountdown.value--
      if (smsCountdown.value <= 0) clearInterval(timer)
    }, 1000)
  } catch (error) {
    ElMessage.error('发送失败')
  }
}

const handleSmsLogin = async () => {
      if (!smsFormRef.value) return
      await smsFormRef.value.validate(async (valid) => {
        if (valid) {
          loading.value = true
          try {
            const res: any = await verifyCode({
              phone: smsForm.phone,
              code: smsForm.code,
              purpose: 0
            })
            if (res.require_2fa) {
              ElMessage.warning('请使用密码登录并输入两步验证码')
            } else if (res.verified) {
              ElMessage.success('登录成功')
              userStore.setToken(res.access_token)
              userStore.setRefreshToken(res.refresh_token || '')
              userStore.setUserInfo({
                id: res.user_id,
                username: res.username,
                email: res.email,
                two_factor_enabled: res.two_factor_enabled || false
              })
              router.push('/account')
            }
          } catch {
          } finally {
            loading.value = false
          }
        }
      })
    }

// 第三方登录
const handleGithubLogin = () => {
  window.location.href = '/api/external/github/authorize'
}

const handleWechatLogin = () => {
  window.location.href = '/api/external/wechat/authorize'
}
</script>

<style scoped>
.login-container {
  width: 100%;
  min-height: 100vh;
  position: relative;
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

.login-box {
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

.login-tabs {
  margin-bottom: 20px;
}

.external-login {
  margin-top: 20px;
}

.external-buttons {
  display: flex;
  justify-content: center;
  gap: 20px;
  margin-top: 15px;
}

.register-link {
  text-align: center;
  margin-top: 20px;
  color: #666;
}

.register-link a {
  color: #409eff;
  text-decoration: none;
}

.register-link a:hover {
  text-decoration: underline;
}

.admin-link {
  text-align: center;
  margin-top: 10px;
  color: #666;
  font-size: 14px;
}

.admin-link a {
  color: #999;
  text-decoration: none;
}

.admin-link a:hover {
  color: #409eff;
  text-decoration: underline;
}
</style>
