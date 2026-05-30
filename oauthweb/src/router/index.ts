import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useAdminStore } from '@/stores/admin'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/Home.vue')
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Register.vue')
  },
  {
    path: '/client/register',
    name: 'ClientRegister',
    component: () => import('@/views/ClientRegister.vue'),
    meta: { requiresUserAuth: true }
  },
  {
    path: '/authorize',
    name: 'Authorize',
    component: () => import('@/views/Authorize.vue'),
    meta: { requiresUserAuth: true }
  },
  {
    path: '/admin/login',
    name: 'AdminLogin',
    component: () => import('@/views/admin/Login.vue')
  },
  {
    path: '/admin',
    name: 'Admin',
    component: () => import('@/views/admin/Index.vue'),
    meta: { requiresAdminAuth: true },
    children: [
      {
        path: '',
        name: 'AdminRoot',
        redirect: { name: 'Dashboard' }
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/admin/Dashboard.vue')
      },
      {
        path: 'clients',
        name: 'Clients',
        component: () => import('@/views/admin/Clients.vue')
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('@/views/admin/Users.vue')
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/admin/Profile.vue')
      }
    ]
  },
  {
    path: '/profile',
    name: 'UserProfile',
    component: () => import('@/views/Profile.vue'),
    meta: { requiresUserAuth: true }
  },
  {
    path: '/dashboard',
    name: 'UserDashboard',
    component: () => import('@/views/Dashboard.vue'),
    meta: { requiresUserAuth: true }
  },
  {
    path: '/apps',
    name: 'MyApps',
    component: () => import('@/views/MyApps.vue'),
    meta: { requiresUserAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, _from, next) => {
  const userStore = useUserStore()
  const adminStore = useAdminStore()
  
  // 普通用户认证路由
  if (to.meta.requiresUserAuth && !userStore.isLoggedIn) {
    next('/login')
    return
  }
  
  // 管理员认证路由
  if (to.meta.requiresAdminAuth && !adminStore.isLoggedIn) {
    next('/admin/login')
    return
  }
  
  next()
})

export default router