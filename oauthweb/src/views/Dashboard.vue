<template>
  <UserLayout>
    <div class="account-container">
      <!-- 已授权的应用 -->
      <el-card class="account-card">
        <template #header>
          <div class="card-header">
            <span>已授权的应用</span>
          </div>
        </template>
        <el-table :data="authorizedApps" v-loading="loading">
          <el-table-column label="Logo" width="55">
            <template #default="{ row }">
              <el-avatar :size="32" :src="row.logo" shape="square">
                {{ row.clientName?.charAt(0)?.toUpperCase() || 'A' }}
              </el-avatar>
            </template>
          </el-table-column>
          <el-table-column prop="clientName" label="应用名称" />
          <el-table-column prop="scope" label="授权范围" />
          <el-table-column prop="createdAt" label="授权时间" />
          <el-table-column label="操作">
            <template #default="{ row }">
              <el-button type="danger" link size="small" @click="handleRevoke(row)">撤销</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <!-- 第三方账号绑定 -->
      <el-card class="account-card">
        <template #header>
          <div class="card-header">
            <span>第三方账号绑定</span>
          </div>
        </template>
        <div class="bind-list">
          <div class="bind-item" v-for="provider in allProviders" :key="provider.key">
            <el-icon><component :is="provider.icon" /></el-icon>
            <span>{{ provider.label }}</span>
            <template v-if="provider.bound">
              <el-tag type="success">已绑定</el-tag>
              <el-button type="danger" link size="small" @click="handleUnbind(provider)">解绑</el-button>
            </template>
            <el-button v-else type="primary" link @click="handleBind(provider.key)">绑定</el-button>
          </div>
        </div>
      </el-card>
    </div>
  </UserLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ChatDotRound } from '@element-plus/icons-vue'
import GithubIcon from '@/components/GithubIcon.vue'
import UserLayout from '@/layouts/UserLayout.vue'
import api from '@/utils/api'

const authorizedApps = ref<any[]>([])
const boundAccounts = ref<any[]>([])
const loading = ref(false)

const allProviders = computed(() => [
  {
    key: 'Github',
    label: 'GitHub',
    icon: GithubIcon,
    bound: boundAccounts.value.some(a => a.provider === 'Github'),
    account: boundAccounts.value.find(a => a.provider === 'Github')
  },
  {
    key: 'Wechat',
    label: '微信',
    icon: ChatDotRound,
    bound: boundAccounts.value.some(a => a.provider === 'Wechat'),
    account: boundAccounts.value.find(a => a.provider === 'Wechat')
  }
])

onMounted(async () => {
  await loadAuthorizedApps()
  await loadBoundProviders()
})

const loadAuthorizedApps = async () => {
  loading.value = true
  try {
    const res: any = await api.get('/user/authorizations')
    authorizedApps.value = res || []
  } catch (error) {
    console.error('Failed to load authorized apps')
  } finally {
    loading.value = false
  }
}

const loadBoundProviders = async () => {
  try {
    const res: any = await api.get('/user/external/accounts')
    boundAccounts.value = res || []
  } catch (error) {
    console.error('Failed to load bound providers')
  }
}

const handleBind = (provider: string) => {
  if (provider === 'Github') {
    window.location.href = '/api/1.0/external/github/authorize?mode=bind'
  } else if (provider === 'Wechat') {
    window.location.href = '/api/1.0/external/wechat/authorize?mode=bind'
  }
}

const handleUnbind = async (provider: any) => {
  try {
    await ElMessageBox.confirm(`确定解除 ${provider.label} 账号绑定吗？`, '提示')
    await api.delete(`/user/external/accounts/${provider.account.id}`)
    ElMessage.success(`${provider.label} 账号已解绑`)
    await loadBoundProviders()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '解绑失败')
    }
  }
}

const handleRevoke = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定撤销该应用的授权吗？', '提示')
    await api.delete(`/user/authorizations/${row.id}`)
    ElMessage.success('已撤销授权')
    await loadAuthorizedApps()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.response?.data?.message || '撤销失败')
    }
  }
}
</script>

<style scoped>
.account-container {
  max-width: 1200px;
  margin: 0 auto;
}

.account-card {
  margin-bottom: 20px;
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header span {
  font-size: 16px;
  font-weight: 600;
}

.info-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 0;
  font-size: 14px;
}

.info-item .label {
  font-weight: 500;
  color: #666;
}

.bind-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.bind-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.bind-item .el-icon {
  font-size: 18px;
  color: #666;
}
</style>