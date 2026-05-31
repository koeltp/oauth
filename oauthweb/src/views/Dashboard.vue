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
          <el-table-column prop="name" label="应用名称" />
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
          <div class="bind-item">
            <el-icon><Link /></el-icon>
            <span>GitHub</span>
            <el-tag v-if="boundProviders.includes('Github')" type="success">已绑定</el-tag>
            <el-button v-else type="primary" link @click="handleBindGithub">绑定</el-button>
          </div>
          <div class="bind-item">
            <el-icon><ChatDotRound /></el-icon>
            <span>微信</span>
            <el-tag v-if="boundProviders.includes('Wechat')" type="success">已绑定</el-tag>
            <el-button v-else type="primary" link @click="handleBindWechat">绑定</el-button>
          </div>
        </div>
      </el-card>
    </div>
  </UserLayout>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Link, ChatDotRound } from '@element-plus/icons-vue'
import UserLayout from '@/layouts/UserLayout.vue'
import api from '@/utils/api'

const authorizedApps = ref<any[]>([])
const boundProviders = ref<string[]>([])
const loading = ref(false)

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
    const res = await api.get('/user/external/accounts')
    boundProviders.value = (res || []).map((a: any) => a.provider)
  } catch (error) {
    console.error('Failed to load bound providers')
  }
}

const handleBindGithub = () => {
  window.location.href = '/api/1.0/external/github/authorize'
}

const handleBindWechat = () => {
  window.location.href = '/api/1.0/external/wechat/authorize'
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