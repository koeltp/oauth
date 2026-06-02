<template>
  <UserLayout>
    <div class="my-apps-container">
      <el-card class="my-apps-card">
        <template #header>
          <div class="card-header">
            <span>我的应用</span>
            <el-button type="primary" size="small" @click="goToRegister">注册新应用</el-button>
          </div>
        </template>
        <el-table :data="clients" v-loading="loading" empty-text="暂无应用" stripe>
          <el-table-column label="Logo" width="60">
            <template #default="{ row }">
              <el-avatar :size="36" :src="row.logo" shape="square">
                {{ row.name?.charAt(0)?.toUpperCase() || 'A' }}
              </el-avatar>
            </template>
          </el-table-column>
          <el-table-column prop="name" label="应用名称" min-width="140" />
          <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
          <el-table-column prop="clientId" label="Client ID" show-overflow-tooltip min-width="250" />
          <el-table-column prop="status" label="状态" width="100">
            <template #default="{ row }">
              <el-tag :type="statusType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="createdAt" label="申请时间" width="170">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="220">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="viewDetail(row)">查看</el-button>
              <el-button v-if="row.status === 'Draft'" type="warning" link size="small" @click="handleEdit(row)">编辑</el-button>
              <el-button v-if="row.status === 'Draft'" type="success" link size="small" @click="handleSubmit(row)">提交审核</el-button>
              <el-button v-if="row.status !== 'Draft'" type="info" link size="small" @click="handleWithdraw(row)">撤回</el-button>
              <el-popconfirm title="确定删除此应用？" @confirm="handleDelete(row)" width="200" confirm-button-text="删除" cancel-button-text="取消">
                <template #reference>
                  <el-button type="danger" link size="small">删除</el-button>
                </template>
              </el-popconfirm>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </div>

    <!-- 查看详情弹窗 -->
    <el-dialog v-model="showDetailDialog" title="应用详情" width="700px">
      <div class="client-detail" v-if="currentClient">
        <div class="detail-row detail-header">
          <el-avatar :size="48" :src="currentClient.logo" shape="square">
            {{ currentClient.name?.charAt(0)?.toUpperCase() || 'A' }}
          </el-avatar>
          <div>
            <div class="detail-title">{{ currentClient.name }}</div>
            <el-tag :type="statusType(currentClient.status)" size="small">{{ statusLabel(currentClient.status) }}</el-tag>
          </div>
        </div>
        <div class="detail-row">
          <span class="detail-label">描述：</span>
          <span class="detail-value">{{ currentClient.description || '-' }}</span>
        </div>
        <div class="detail-row">
          <span class="detail-label">Client ID：</span>
          <span class="detail-value">{{ currentClient.clientId }}</span>
          <el-button link @click="copyToClipboard(currentClient.clientId)">
            <el-icon><CopyDocument /></el-icon>
          </el-button>
        </div>
        <div v-if="currentClient.status === 'Approved' && currentClient.clientSecret" class="detail-row">
          <span class="detail-label">Client Secret：</span>
          <span class="detail-value secret">{{ maskSecret(currentClient.clientSecret) }}</span>
          <el-button link @click="copyToClipboard(currentClient.clientSecret)">
            <el-icon><CopyDocument /></el-icon>
          </el-button>
          <el-button link @click="downloadSecret(currentClient.clientSecret, currentClient.name, currentClient.clientId)">
            <el-icon><Download /></el-icon>
          </el-button>
        </div>
        <div class="detail-row">
          <span class="detail-label">状态：</span>
          <el-tag :type="statusType(currentClient.status)" size="small">{{ statusLabel(currentClient.status) }}</el-tag>
        </div>
        <div class="detail-row">
          <span class="detail-label">回调地址：</span>
          <span class="detail-value">{{ currentClient.redirectUris || '-' }}</span>
        </div>
        <div class="detail-row">
          <span class="detail-label">授权范围：</span>
          <span class="detail-value">
            <el-tag v-for="scope in (currentClient.allowedScopes || '').split(' ')" :key="scope" size="small" style="margin-right: 6px">{{ scope }}</el-tag>
            <span v-if="!currentClient.allowedScopes">-</span>
          </span>
        </div>
        <div class="detail-row">
          <span class="detail-label">申请时间：</span>
          <span class="detail-value">{{ formatDate(currentClient.createdAt) }}</span>
        </div>
      </div>
    </el-dialog>
  </UserLayout>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { CopyDocument, Download } from '@element-plus/icons-vue'
import UserLayout from '@/layouts/UserLayout.vue'
import api from '@/utils/api'
import { formatDate } from '@/utils/format'
import { getClientStatusType, getClientStatusText } from '@/constants'

const router = useRouter()
const clients = ref<any[]>([])
const loading = ref(false)

const statusType = getClientStatusType
const statusLabel = getClientStatusText

// 加载列表
const loadClients = async () => {
  loading.value = true
  try {
    const res: any = await api.get('/clients/my')
    clients.value = res || []
  } catch (error) {
    console.error('Failed to load clients')
  } finally {
    loading.value = false
  }
}

onMounted(loadClients)

const goToRegister = () => {
  router.push('/client/register')
}

// 查看详情
const showDetailDialog = ref(false)
const currentClient = ref<any>(null)

const viewDetail = (client: any) => {
  currentClient.value = client
  showDetailDialog.value = true
}

// 脱敏显示 Secret
const maskSecret = (secret: string) => {
  if (!secret || secret.length <= 12) return secret
  return secret.slice(0, 6) + '***********************' + secret.slice(-4)
}

const copyToClipboard = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text)
    ElMessage.success('已复制到剪贴板')
  } catch {
    ElMessage.error('复制失败')
  }
}

const downloadSecret = (secret: string, appName: string, clientId: string) => {
  try {
    const content = `OAuth 客户端密钥\n\n应用名称: ${appName}\nClient ID: ${clientId}\nClient Secret: ${secret}\n\n请妥善保管此密钥，用于 OAuth 授权接入。`
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `client-secret-${clientId}.txt`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
    ElMessage.success('密钥已下载')
  } catch {
    ElMessage.error('下载失败')
  }
}

const handleEdit = (client: any) => {
  router.push({ path: '/client/register', query: { edit: client.id } })
}

// 提交审核
const handleSubmit = async (client: any) => {
  try {
    await api.post(`/clients/${client.id}/submit`)
    ElMessage.success('已提交审核')
    await loadClients()
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '提交失败')
  }
}

// 撤回
const handleWithdraw = async (client: any) => {
  try {
    await api.post(`/clients/${client.id}/withdraw`)
    ElMessage.success('已撤回，可重新编辑')
    await loadClients()
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '撤回失败')
  }
}

// 删除
const handleDelete = async (client: any) => {
  try {
    await api.delete(`/clients/${client.id}`)
    ElMessage.success('删除成功')
    await loadClients()
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '删除失败')
  }
}
</script>

<style scoped>
.my-apps-container {
  max-width: 1200px;
  margin: 0 auto;
}

.my-apps-card {
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

.client-detail {
  padding: 10px 0;
}

.detail-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 0;
  font-size: 14px;
}

.detail-label {
  font-weight: 600;
  color: #666;
  min-width: 90px;
}

.detail-value {
  flex: 1;
  color: #333;
  word-break: break-all;
}

.detail-value.secret {
  font-family: 'Courier New', monospace;
  font-size: 12px;
  background: #f5f7fa;
  padding: 4px 8px;
  border-radius: 4px;
  color: #e74c3c;
  user-select: all;
}
</style>