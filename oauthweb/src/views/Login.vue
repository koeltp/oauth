<template>
  <div class="login-container">
    <div class="login-box">
      <router-link to="/" class="home-icon" title="返回首页">
        <el-icon><HomeFilled /></el-icon>
      </router-link>
      <h1 class="title">
        <img src="@/assets/logo.png" height="32" alt="logo" style="margin-right: 8px;" />
        OAuth Server
      </h1>

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
        还没有账号？ <router-link to="/register">立即注册</router-link>
      </div>

      <div class="admin-link">
        <el-icon style="margin-right: 4px; vertical-align: middle;" :size="14"><UserFilled /></el-icon>
        <a href="https://ssoadmin.taipi.top" target="_blank">管理员登录</a>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ChatDotRound, HomeFilled, UserFilled } from '@element-plus/icons-vue'
import GithubIcon from '@/components/GithubIcon.vue'
import PasswordLoginForm from '@/components/login/PasswordLoginForm.vue'
import EmailCodeLoginForm from '@/components/login/EmailCodeLoginForm.vue'
import SmsCodeLoginForm from '@/components/login/SmsCodeLoginForm.vue'

const route = useRoute()
const activeTab = ref('password')

function onTwoFaRequired(_userId: string) {
  ElMessage.info('请完成两步验证')
  activeTab.value = 'password'
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