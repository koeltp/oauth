<template>
  <div class="admin-layout">
    <div class="top-header">
      <div class="header-left">
        <h1>OAuth 管理后台</h1>
      </div>
      <div class="header-right">
        <div class="user-avatar-wrapper" @click="toggleUserMenu">
          <div class="user-avatar">
            <img 
              v-if="adminStore.adminInfo?.avatarUrl && !avatarLoadFailed" 
              :src="adminStore.adminInfo.avatarUrl" 
              @error="() => (avatarLoadFailed = true)" 
            />
            <span v-else>{{ adminStore.avatarText }}</span>
          </div>
          <span class="user-name">{{ adminStore.adminInfo?.username || '管理员' }}</span>
          <svg class="arrow-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M6 9l6 6 6-6" />
          </svg>
        </div>
        <div v-if="showUserMenu" class="user-menu" @click.stop>
          <div class="menu-item" @click="handleSettings">
         <el-icon><User  /></el-icon>
            <span>个人中心</span>
          </div>
          <div class="menu-item logout" @click="handleLogout">
            <el-icon><SwitchButton /></el-icon>
            <span>退出登录</span>
          </div>
        </div>
      </div>
    </div>
    <div class="main-body">
      <div class="sidebar">
        <div class="menu-list">
          <div 
            v-for="item in menuItems" 
            :key="item.path"
            :class="['menu-item', { active: activeMenu === item.path }]"
            @click="handleMenuClick(item.path)"
          >
            <el-icon class="menu-icon"><component :is="item.icon" /></el-icon>
            <span>{{ item.name }}</span>
          </div>
        </div>
      </div>
      <div class="main-content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, markRaw, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Grid , Monitor , User, SwitchButton } from '@element-plus/icons-vue'
import { useAdminStore } from '@/stores/admin'

const adminStore = useAdminStore()
const route = useRoute()
const router = useRouter()
const activeMenu = computed(() => route.path)
const showUserMenu = ref(false)
const avatarLoadFailed = ref(false)

const menuItems = ref([
  { name: '仪表盘', path: '/dashboard', icon: markRaw(Grid)  },
  { name: '客户端管理', path: '/clients', icon: markRaw(Monitor)  },
  { name: '用户管理', path: '/users', icon: markRaw(User) },
  { name: '管理员管理', path: '/admins', icon: markRaw(User) }
])

watch(() => adminStore.adminInfo?.avatarUrl, () => {
  avatarLoadFailed.value = false
})

const handleMenuClick = (path: string) => {
  router.push(path)
}

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value
}

const handleSettings = () => {
  showUserMenu.value = false
  router.push('/profile')
}

const handleLogout = () => {
  showUserMenu.value = false
  adminStore.logout()
  ElMessage.success('退出成功')
  router.push('/login')
}
</script>

<style scoped>
.admin-layout {
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100vh;
}

.top-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 60px;
  padding: 0 20px;
  background-color: #fff;
  border-bottom: 1px solid #eee;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  position: relative;
}

.header-left h1 {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.user-avatar-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  border-radius: 20px;
  cursor: pointer;
  transition: all 0.3s;
}

.user-avatar-wrapper:hover {
  background-color: #f5f7fa;
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  overflow: hidden;
}

.user-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.user-name {
  font-size: 14px;
  color: #333;
}

.arrow-icon {
  width: 16px;
  height: 16px;
  color: #999;
  transition: transform 0.3s;
}

.user-avatar-wrapper:hover .arrow-icon,
.user-avatar-wrapper.active .arrow-icon {
  transform: rotate(180deg);
}

.user-menu {
  position: absolute;
  top: calc(100% + 8px);
  right: 20px;
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.12);
  padding: 8px 0;
  min-width: 160px;
  z-index: 100;
}

.user-menu .menu-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 16px;
  font-size: 14px;
  color: #333;
  cursor: pointer;
  transition: background-color 0.2s;
}

.user-menu .menu-item:hover {
  background-color: #f5f7fa;
}

.user-menu .menu-item svg {
  width: 18px;
  height: 18px;
  color: #666;
}

.user-menu .menu-item.logout {
  color: #e74c3c;
}

.user-menu .menu-item.logout svg {
  color: #e74c3c;
}

.main-body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.sidebar {
  width: 200px;
  background-color: #304156;
}

.menu-list {
  padding: 10px 0;
}

.menu-list .menu-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px;
  color: #bfcbd9;
  cursor: pointer;
  transition: all 0.3s;
}

.menu-list .menu-item:hover {
  background-color: #2a3f5f;
  color: #fff;
}

.menu-list .menu-item.active {
  background-color: #2a3f5f;
  color: #409eff;
  border-left: 3px solid #409eff;
}

.menu-icon {
  width: 18px;
  height: 18px;
  color: #bfcbd9;
  flex-shrink: 0;
}

.menu-item:hover .menu-icon,
.menu-item.active .menu-icon {
  color: #409eff;
}

.main-content {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  background-color: #f5f7fa;
}
</style>
