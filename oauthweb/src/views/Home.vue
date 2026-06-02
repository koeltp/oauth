<template>
  <div class="home-container">
    <nav class="navbar">
      <div class="navbar-inner">
        <router-link to="/" class="navbar-brand">
          <img src="@/assets/logo.png" alt="TP OAuth" class="navbar-logo" />
          <span class="navbar-title">TP OAuth</span>
        </router-link>
        <div class="navbar-right">
          <router-link to="/" class="nav-link">首页</router-link>
          <router-link to="/docs" class="nav-link">文档</router-link>
          <router-link to="/login" class="nav-link">登录</router-link>
          <router-link to="/register" class="nav-link">注册</router-link>
        </div>
      </div>
    </nav>

    <div class="hero-section">
      <div class="hero-content">
        <h1 class="hero-title">TP OAuth 授权服务</h1>
        <p class="hero-subtitle">安全、可靠的统一身份认证平台</p>
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
import { Lock, Connection, Monitor , User, Setting, CircleCheck , Odometer   } from '@element-plus/icons-vue'
import api from '@/utils/api'

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
</script>

<style scoped>
.home-container {
  min-height: 100vh;
  background: white;
}

.navbar {
  display: flex;
  align-items: center;
  height: 60px;
  border-bottom: 1px solid #f0f0f0;
  position: sticky;
  top: 0;
  background: white;
  z-index: 100;
}

.navbar-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}

.navbar-logo {
  height: 32px;
}

.navbar-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 24px;
}

.nav-link {
  font-size: 15px;
  color: #666;
  text-decoration: none;
  transition: color 0.2s;
}

.nav-link:hover {
  color: #409eff;
}

.nav-link.router-link-active {
  color: #409eff;
  font-weight: 500;
}

.hero-section {
  padding: 100px 20px 80px;
  text-align: center;
  background: linear-gradient(135deg, #f0f5ff 0%, #e8f0fe 50%, #f5f0ff 100%);
  position: relative;
  overflow: hidden;
}

.hero-section::before {
  content: '';
  position: absolute;
  top: -200px;
  right: -200px;
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(64,158,255,0.08) 0%, transparent 70%);
  border-radius: 50%;
}

.hero-section::after {
  content: '';
  position: absolute;
  bottom: -150px;
  left: -150px;
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(103,194,58,0.06) 0%, transparent 70%);
  border-radius: 50%;
}

.hero-content {
  max-width: 800px;
  margin: 0 auto 60px;
  position: relative;
  z-index: 1;
}

.hero-title {
  font-size: 52px;
  font-weight: 800;
  margin-bottom: 16px;
  background: linear-gradient(135deg, #2b5fd9 0%, #7c3aed 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -1px;
}

.hero-subtitle {
  font-size: 20px;
  color: #666;
  margin-bottom: 40px;
  font-weight: 400;
}

.hero-features {
  display: flex;
  gap: 24px;
  justify-content: center;
  flex-wrap: nowrap;
  max-width: 1200px;
  margin: 0 auto;
  position: relative;
  z-index: 1;
}

.feature-item {
  flex: 1;
  padding: 36px 28px;
  background: white;
  border-radius: 16px;
  border: 1px solid #eef2f6;
  transition: all 0.3s ease;
}

.feature-item:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.08);
  border-color: transparent;
}

.feature-icon {
  font-size: 40px;
  margin-bottom: 16px;
  color: #409eff;
}

.feature-item h3 {
  font-size: 18px;
  margin-bottom: 8px;
  color: #1a1a2e;
}

.feature-item p {
  font-size: 14px;
  color: #888;
  line-height: 1.6;
}

.stats-section {
  padding: 60px 20px;
}

.stats-container {
  display: flex;
  gap: 24px;
  justify-content: center;
  max-width: 1200px;
  margin: 0 auto;
}

.stat-card {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 28px;
  background: white;
  border-radius: 16px;
  border: 1px solid #eef2f6;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
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
  color: #1a1a2e;
}

.stat-label {
  font-size: 14px;
  color: #888;
}

.info-section {
  max-width: 1200px;
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
  max-width: 1200px;
  margin: 0 auto;
}
</style>
