<template>
  <div class="user-layout">
    <header class="top-nav">
      <div class="nav-inner">
        <router-link to="/dashboard" class="logo">
          <img src="@/assets/logo.png" height="28" alt="logo" />
          <span>OAuth</span>
        </router-link>
        <div class="nav-right">
          <el-menu mode="horizontal" :ellipsis="false" class="nav-menu" router>
            <el-menu-item index="/dashboard">首页</el-menu-item>
            <el-menu-item index="/apps">我的应用</el-menu-item>
          </el-menu>
          <el-dropdown trigger="click" @command="handleCommand">
            <span class="user-trigger">
              <span class="user-name">{{ userStore.userInfo?.username || '用户' }}</span>
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile">个人中心</el-dropdown-item>
                <el-dropdown-item command="logout" divided>退出</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </header>
    <main class="main-content">
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowDown } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const userStore = useUserStore()

const handleCommand = (command: string) => {
  if (command === 'profile') {
    router.push('/profile')
  } else if (command === 'logout') {
    userStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  }
}
</script>

<style scoped>
.user-layout {
  width: 100%;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f5f7fa;
}

.top-nav {
  display: flex;
  justify-content: center;
  height: 60px;
  background: #fff;
  border-bottom: 1px solid #eee;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  position: sticky;
  top: 0;
  z-index: 100;
}

.nav-inner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  max-width: 1200px;
  padding: 0 24px;
}

.logo {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
  color: #333;
  font-size: 18px;
  font-weight: 600;
}

.logo .el-icon {
  color: #409eff;
}

.nav-menu {
  border-bottom: none !important;
  background: transparent !important;
  height: 60px;
  margin-right: 12px;
}

.nav-menu :deep(.el-menu-item) {
  border-bottom: none !important;
  height: 60px;
  line-height: 60px;
  font-size: 14px;
}

.nav-right {
  display: flex;
  align-items: center;
}

.user-trigger {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 6px 12px;
  border-radius: 8px;
  transition: background-color 0.2s;
}

.user-trigger:hover {
  background-color: #f5f7fa;
}

.user-name {
  font-size: 14px;
  color: #333;
}

.main-content {
  flex: 1;
  padding: 24px;
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
}
</style>