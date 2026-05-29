<template>
  <div class="home-container">
    <div class="hero-section">
      <div class="hero-content">
        <h1 class="hero-title">OAuth 授权服务</h1>
        <p class="hero-subtitle">安全、可靠的统一身份认证平台</p>
        <div class="hero-actions">
          <el-button type="primary" size="large" @click="goToLogin">登录</el-button>
          <el-button size="large" @click="goToRegister">注册账号</el-button>
        </div>
      </div>
      <div class="hero-features">
        <div class="feature-item">
          <el-icon class="feature-icon"><Lock /></el-icon>
          <h3>安全认证</h3>
          <p>支持两步验证，保护您的账户安全</p>
        </div>
        <div class="feature-item">
          <el-icon class="feature-icon"><Connection /></el-icon>
          <h3>统一登录</h3>
          <p>一次登录，访问所有接入应用</p>
        </div>
        <div class="feature-item">
          <el-icon class="feature-icon"><Key /></el-icon>
          <h3>OAuth 2.0</h3>
          <p>标准协议，安全授权第三方应用</p>
        </div>
      </div>
    </div>

    <div class="stats-section">
      <div class="stats-container">
        <div class="stat-card">
          <el-icon class="stat-icon" color="#409eff"><User /></el-icon>
          <div class="stat-content">
            <span class="stat-value">{{ stats.totalUsers }}</span>
            <span class="stat-label">注册用户</span>
          </div>
        </div>
        <div class="stat-card">
          <el-icon class="stat-icon" color="#67c23a"><Monitor  /></el-icon>
          <div class="stat-content">
            <span class="stat-value">{{ stats.totalClients }}</span>
            <span class="stat-label">OAuth 客户端</span>
          </div>
        </div>
        <div class="stat-card">
          <el-icon class="stat-icon" color="#e6a23c"><CircleCheck  /></el-icon>
          <div class="stat-content">
            <span class="stat-value">{{ stats.totalAuthorizations }}</span>
            <span class="stat-label">授权次数</span>
          </div>
        </div>
        <div class="stat-card">
          <el-icon class="stat-icon" color="#409eff"><Odometer   /></el-icon>
          <div class="stat-content">
            <span class="stat-value">{{ stats.activeClients }}</span>
            <span class="stat-label">活跃应用</span>
          </div>
        </div>
      </div>
    </div>

    <div class="info-section">
      <el-row :gutter="30">
        <el-col :span="12">
          <el-card shadow="hover" class="info-card">
            <template #header>
              <div class="card-header">
                <el-icon><User /></el-icon>
                <span>用户功能</span>
              </div>
            </template>
            <ul class="feature-list">
              <li>多方式登录（密码、邮箱验证码、短信验证码）</li>
              <li>两步验证（2FA）保护账户安全</li>
              <li>第三方登录（微信、GitHub）</li>
              <li>管理已授权的应用</li>
              <li>注册 OAuth 客户端</li>
            </ul>
          </el-card>
        </el-col>
        <el-col :span="12">
          <el-card shadow="hover" class="info-card">
            <template #header>
              <div class="card-header">
                <el-icon><Setting /></el-icon>
                <span>开发者功能</span>
              </div>
            </template>
            <ul class="feature-list">
              <li>注册 OAuth 2.0 客户端</li>
              <li>获取 Client ID 和 Client Secret</li>
              <li>配置回调地址和权限范围</li>
              <li>支持 Authorization Code 流程</li>
              <li>支持 PKCE（移动端安全）</li>
            </ul>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <div class="footer-section">
      <p>&copy; 2024 OAuth 授权服务. All rights reserved.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Lock, Connection, Monitor , User, Setting, CircleCheck , Odometer   } from '@element-plus/icons-vue'
import api from '@/utils/api'

const router = useRouter()

const stats = ref({
  totalUsers: 0,
  totalClients: 0,
  totalAuthorizations: 0,
  activeClients: 0
})

onMounted(async () => {
  try {
    const res = await api.get('/stats')
    stats.value = res || {
      totalUsers: 0,
      totalClients: 0,
      totalAuthorizations: 0,
      activeClients: 0
    }
  } catch (error) {
    // 使用默认模拟数据
    stats.value = {
      totalUsers: 0,
      totalClients: 0,
      totalAuthorizations: 0,
      activeClients: 0
    }
  }
})

const goToLogin = () => {
  router.push('/login')
}

const goToRegister = () => {
  router.push('/register')
}
</script>

<style scoped>
.home-container {
  min-height: 100vh;
  background: white;
}

.hero-section {
  padding: 80px 20px;
  text-align: center;
  color: #333;
}

.hero-content {
  max-width: 600px;
  margin: 0 auto 60px;
}

.hero-title {
  font-size: 48px;
  font-weight: 700;
  margin-bottom: 20px;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

.hero-subtitle {
  font-size: 20px;
  opacity: 0.9;
  margin-bottom: 40px;
}

.hero-actions {
  display: flex;
  gap: 20px;
  justify-content: center;
}

.hero-actions .el-button {
  min-width: 120px;
}

.hero-features {
  display: flex;
  gap: 20px;
  justify-content: space-between;
  flex-wrap: nowrap;
  max-width: 1000px;
  margin: 0 auto;
}

.feature-item {
  flex: 1;
  min-width: 280px;
  max-width: 340px;
  padding: 30px;
  background: #f5f7fa;
  border-radius: 12px;
}

.feature-icon {
  font-size: 40px;
  margin-bottom: 15px;
}

.stats-section {
  padding: 40px 20px;
  background: #fafafa;
}

.stats-container {
  display: flex;
  gap: 20px;
  justify-content: space-between;
  max-width: 1000px;
  margin: 0 auto;
}

.stat-card {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 25px;
  background: white;
  border-radius: 12px;
  border: 1px solid #e4e7ed;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.stat-icon {
  font-size: 36px;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: #333;
}

.stat-label {
  font-size: 14px;
  color: #666;
}

.feature-item h3 {
  font-size: 18px;
  margin-bottom: 10px;
}

.feature-item p {
  font-size: 14px;
  opacity: 0.8;
}

.info-section {
  max-width: 1000px;
  margin: 0 auto;
  padding: 60px 0;
}

.info-card {
  height: 100%;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 18px;
  font-weight: 600;
}

.card-header .el-icon {
  font-size: 24px;
  color: #409eff;
}

.feature-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.feature-list li {
  padding: 10px 0;
  padding-left: 20px;
  position: relative;
  color: #666;
  border-bottom: 1px solid #f0f0f0;
}

.feature-list li:last-child {
  border-bottom: none;
}

.feature-list li::before {
  content: '✓';
  position: absolute;
  left: 0;
  color: #67c23a;
  font-weight: bold;
}

.footer-section {
  text-align: center;
  padding: 40px 20px;
  color: #666;
  font-size: 14px;
}
</style>
