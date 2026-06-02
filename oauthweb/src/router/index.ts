import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'

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
    path: '/forgot-password',
    name: 'ForgotPassword',
    component: () => import('@/views/ForgotPassword.vue')
  },
  {
    path: '/verify-email',
    name: 'VerifyEmail',
    component: () => import('@/views/VerifyEmail.vue')
  },
  {
    path: '/auth/github/callback',
    name: 'GithubCallback',
    component: () => import('@/views/GithubCallback.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Register.vue')
  },
  {
    path: '/docs',
    name: 'Docs',
    component: () => import('@/views/Docs.vue')
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
  
  if (to.name === 'Login' && userStore.isLoggedIn) {
    next('/dashboard')
    return
  }
  
  if (to.meta.requiresUserAuth && !userStore.isLoggedIn) {
    next({ path: '/login', query: { redirect: to.fullPath } })
    return
  }
  
  next()
})

export default router