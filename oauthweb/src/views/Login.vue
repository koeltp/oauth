<template>
  <div class="login-container">
    <div class="bubble bubble-1"></div>
    <div class="bubble bubble-2"></div>
    <div class="bubble bubble-3"></div>
    <div class="bubble bubble-4"></div>
    <div class="bubble bubble-5"></div>

    <div class="login-box">
      <router-link to="/" class="home-icon" title="返回首页">
        <el-icon><HomeFilled /></el-icon>
      </router-link>
      <div class="logo-area">
        <img src="@/assets/logo.png" alt="logo" />
      </div>
      <h1 class="title">TP OAuth 登录</h1>

      <template v-if="showTwoFa">
        <p class="twofa-title">两步验证</p>
        <p class="twofa-hint">此账户已启用两步验证<br/>请输入 Authenticator 中的验证码</p>
        <TwoFactorLoginForm :user-id="twoFaUserId" @cancel="handleTwoFaCancel" />
      </template>

      <template v-else>
        <el-tabs v-model="activeTab" class="login-tabs">
          <el-tab-pane label="密码登录" name="password">
            <PasswordLoginForm @two-fa-required="onTwoFaRequired" />
          </el-tab-pane>
          <el-tab-pane label="邮箱验证码" name="email">
            <EmailCodeLoginForm />
          </el-tab-pane>
          <el-tab-pane label="短信验证码" name="sms">
            <SmsCodeLoginForm />
          </el-tab-pane>
        </el-tabs>

        <div class="external-login">
          <el-divider>其他登录方式</el-divider>
          <div class="external-buttons">
            <el-button @click="handleGithubLogin" circle size="large">
              <GithubIcon />
            </el-button>
            <el-button @click="handleWechatLogin" :icon="ChatDotRound" circle size="large" />
          </div>
        </div>

        <div class="register-link">
          还没有账号？ <router-link :to="{ path: '/register', query: $route.query.redirect ? { redirect: $route.query.redirect } : {} }">立即注册</router-link>
        </div>

        <div class="forgot-link">
          <router-link :to="{ path: '/forgot-password', query: $route.query.redirect ? { redirect: $route.query.redirect } : {} }">忘记密码？</router-link>
        </div>

        <div class="admin-link">
          <el-icon style="margin-right: 4px; vertical-align: middle;" :size="14"><UserFilled /></el-icon>
          <a href="https://ssoadmin.taipi.top" target="_blank">管理员登录</a>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { ChatDotRound, HomeFilled, UserFilled } from '@element-plus/icons-vue'
import GithubIcon from '@/components/GithubIcon.vue'
import PasswordLoginForm from '@/components/login/PasswordLoginForm.vue'
import EmailCodeLoginForm from '@/components/login/EmailCodeLoginForm.vue'
import SmsCodeLoginForm from '@/components/login/SmsCodeLoginForm.vue'
import TwoFactorLoginForm from '@/components/login/TwoFactorLoginForm.vue'

const route = useRoute()
const activeTab = ref('password')

const showTwoFa = ref(false)
const twoFaUserId = ref('')

function onTwoFaRequired(userId: string) {
  twoFaUserId.value = userId
  showTwoFa.value = true
}

function handleTwoFaCancel() {
  showTwoFa.value = false
  twoFaUserId.value = ''
}

function handleGithubLogin() {
  const redirectUrl = route.query.redirect as string || ''
  let authorizeUrl = '/api/1.0/external/github/authorize?mode=login'
  if (redirectUrl) {
    authorizeUrl += `&redirect_url=${encodeURIComponent(redirectUrl)}`
  }
  window.location.href = authorizeUrl
}

function handleWechatLogin() {
  const redirectUrl = route.query.redirect as string || ''
  let authorizeUrl = '/api/1.0/external/wechat/authorize?mode=login'
  if (redirectUrl) {
    authorizeUrl += `&redirect_url=${encodeURIComponent(redirectUrl)}`
  }
  window.location.href = authorizeUrl
}
</script>

<style scoped>
.login-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
}

.login-container::before {
  content: '';
  position: absolute;
  top: -250px;
  right: -200px;
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, rgba(64,158,255,0.1) 0%, transparent 70%);
  border-radius: 50%;
}

.login-container::after {
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

.login-box {
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

.forgot-link {
  text-align: center;
  margin-top: 10px;
}

.forgot-link a {
  color: #999;
  text-decoration: none;
  font-size: 13px;
}

.forgot-link a:hover {
  color: #409eff;
  text-decoration: underline;
}

.twofa-title {
  text-align: center;
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 8px;
}

.twofa-hint {
  text-align: center;
  font-size: 14px;
  color: #999;
  margin-bottom: 20px;
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