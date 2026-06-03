<template>
  <div class="register-container">
    <div class="bubble bubble-1"></div>
    <div class="bubble bubble-2"></div>
    <div class="bubble bubble-3"></div>
    <div class="bubble bubble-4"></div>
    <div class="bubble bubble-5"></div>

    <div class="register-box">
      <router-link to="/" class="home-icon" title="返回首页">
        <el-icon><HomeFilled /></el-icon>
      </router-link>
      <div class="logo-area">
        <img src="@/assets/logo.png" alt="logo" />
      </div>
      <h1 class="title">TP OAuth 注册</h1>
      
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <el-form-item prop="username">
          <el-input v-model="form.username" placeholder="用户名" size="large" :prefix-icon="User" />
        </el-form-item>
        <el-form-item prop="email">
          <el-input v-model="form.email" placeholder="邮箱" size="large" :prefix-icon="Message">
            <template #append>
              <el-button @click="sendCode" :disabled="countdown > 0">
                {{ countdown > 0 ? `${countdown}s` : '获取验证码' }}
              </el-button>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item prop="code">
          <el-input v-model="form.code" placeholder="邮箱验证码" size="large" :prefix-icon="Key" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="form.password" type="password" placeholder="密码" size="large" :prefix-icon="Lock" show-password />
        </el-form-item>
        <el-form-item prop="confirmPassword">
          <el-input v-model="form.confirmPassword" type="password" placeholder="确认密码" size="large" :prefix-icon="Lock" show-password />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="large" style="width: 100%" @click="handleRegister" :loading="loading">
            注册
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-link">
        已有账号？ <router-link :to="{ path: '/login', query: $route.query.redirect ? { redirect: $route.query.redirect } : {} }">立即登录</router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User, Message, Key, Lock, HomeFilled } from '@element-plus/icons-vue'
import api from '@/utils/api'

const router = useRouter()
const route = useRoute()

const formRef = ref<FormInstance>()
const loading = ref(false)
const countdown = ref(0)

const form = reactive({
  username: '',
  email: '',
  code: '',
  password: '',
  confirmPassword: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (value !== form.password) {
    callback(new Error('两次输入密码不一致'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 20, message: '用户名长度在 3 到 20 个字符', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于 6 位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

const sendCode = async () => {
  if (!form.email) {
    ElMessage.warning('请输入邮箱')
    return
  }
  try {
    await api.post('/auth/login/email-code', {
      email: form.email,
      purpose: 1 // Register
    })
    ElMessage.success('验证码已发送')
    countdown.value = 60
    const timer = setInterval(() => {
      countdown.value--
      if (countdown.value <= 0) clearInterval(timer)
    }, 1000)
  } catch (error) {
    ElMessage.error('发送失败')
  }
}

const handleRegister = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        await api.post('/auth/register', {
          username: form.username,
          email: form.email,
          password: form.password
        })
        ElMessage.success('注册成功')
        // 保留 OAuth 授权参数，注册后跳回登录页并携带 redirect
        const redirect = route.query.redirect as string
        if (redirect) {
          router.push({ path: '/login', query: { redirect } })
        } else {
          router.push('/login')
        }
      } catch (error: any) {
        ElMessage.error(error.response?.data?.message || '注册失败')
      } finally {
        loading.value = false
      }
    }
  })
}
</script>

<style scoped>
.register-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
  padding: 40px 20px;
}

.register-container::before {
  content: '';
  position: absolute;
  top: -250px;
  right: -200px;
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, rgba(64,158,255,0.1) 0%, transparent 70%);
  border-radius: 50%;
}

.register-container::after {
  content: '';
  position: absolute;
  bottom: -180px;
  left: -150px;
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(103,194,58,0.07) 0%, transparent 70%);
  border-radius: 50%;
}

.bubble {
  position: absolute;
  border-radius: 50%;
  z-index: 0;
  animation: float 6s ease-in-out infinite;
}

.bubble-1 {
  width: 80px;
  height: 80px;
  top: 15%;
  left: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(64,158,255,0.12), rgba(64,158,255,0.04));
  animation-delay: 0s;
}

.bubble-2 {
  width: 120px;
  height: 120px;
  top: 60%;
  left: 5%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.1), rgba(124,58,237,0.03));
  animation-delay: 1s;
  animation-duration: 8s;
}

.bubble-3 {
  width: 60px;
  height: 60px;
  top: 25%;
  right: 12%;
  background: radial-gradient(circle at 30% 30%, rgba(103,194,58,0.1), rgba(103,194,58,0.03));
  animation-delay: 2s;
  animation-duration: 7s;
}

.bubble-4 {
  width: 100px;
  height: 100px;
  top: 70%;
  right: 8%;
  background: radial-gradient(circle at 30% 30%, rgba(64,158,255,0.08), rgba(64,158,255,0.02));
  animation-delay: 0.5s;
  animation-duration: 9s;
}

.bubble-5 {
  width: 40px;
  height: 40px;
  top: 45%;
  left: 50%;
  background: radial-gradient(circle at 30% 30%, rgba(124,58,237,0.12), rgba(124,58,237,0.04));
  animation-delay: 3s;
  animation-duration: 6s;
}

@keyframes float {
  0%, 100% { transform: translateY(0) scale(1); }
  50% { transform: translateY(-20px) scale(1.05); }
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
  z-index: 1;
}

.home-icon:hover {
  color: #409eff;
  background: #f0f5ff;
}

.register-box {
  position: relative;
  width: 450px;
  margin: 0 auto;
  padding: 40px;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.08);
  border: 1px solid #f0f2f5;
  z-index: 1;
}

.logo-area {
  text-align: center;
  margin-bottom: 16px;
}

.logo-area img {
  height: 56px;
}

.title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 28px;
  font-weight: 600;
}

.login-link {
  text-align: center;
  margin-top: 20px;
  color: #666;
}

.login-link a {
  color: #409eff;
  text-decoration: none;
}

.login-link a:hover {
  text-decoration: underline;
}
</style>
