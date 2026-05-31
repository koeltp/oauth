<template>
  <div class="clients-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">客户端管理</span>
          <el-button type="primary" @click="showRegisterDialog = true">
            <el-icon><Plus /></el-icon>
            新建客户端
          </el-button>
        </div>
      </template>
      
      <!-- 搜索区域 -->
      <div class="search-area">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="应用名称">
            <el-input v-model="searchForm.name" placeholder="请输入应用名称" clearable />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 150px">
              <el-option label="待审核" value="Pending" />
              <el-option label="已批准" value="Approved" />
              <el-option label="已拒绝" value="Rejected" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">
              <el-icon><Search /></el-icon>
              搜索
            </el-button>
            <el-button @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 表格区域 -->
      <el-table :data="clients" v-loading="loading" stripe style="margin-top: 20px">
        <el-table-column type="index" label="序号" width="80" align="center" />
        <el-table-column prop="name" label="应用名称" min-width="150" />
        <el-table-column prop="description" label="应用描述" min-width="200" show-overflow-tooltip />
        <el-table-column prop="clientId" label="Client ID" width="200" />
        <el-table-column prop="redirectUris" label="回调地址" min-width="200" show-overflow-tooltip />
        <el-table-column prop="allowedScopes" label="授权范围" min-width="200">
          <template #default="{ row }">
            <el-tag v-for="scope in row.allowedScopes?.split(' ') || []" :key="scope" size="small" style="margin-right: 4px">
              {{ scope }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getClientStatusType(row.status)" size="small">
              {{ getClientStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="250" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="success" link size="small" @click="handleApprove(row)" v-if="row.status === 'Pending'">
              批准
            </el-button>
            <el-button type="danger" link size="small" @click="handleReject(row)" v-if="row.status === 'Pending'">
              拒绝
            </el-button>
            <el-button type="primary" link size="small" @click="handleView(row)" v-if="row.status !== 'Pending'">详情</el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页区域 -->
      <div class="pagination-area">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>

    <!-- 新建客户端对话框 -->
    <el-dialog v-model="showRegisterDialog" title="新建客户端" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="应用名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入应用名称" />
        </el-form-item>
        <el-form-item label="回调地址" prop="redirectUris">
          <el-input v-model="form.redirectUris" type="textarea" :rows="3" placeholder="多个地址用换行分隔" />
        </el-form-item>
        <el-form-item label="授权范围" prop="allowedScopes">
          <el-input v-model="form.allowedScopes" placeholder="默认: openid profile email" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRegisterDialog = false">取消</el-button>
        <el-button type="primary" @click="handleRegister">创建</el-button>
      </template>
    </el-dialog>

    <!-- 客户端详情对话框 -->
    <el-dialog v-model="showDetailDialog" title="客户端详情" width="600px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="应用名称">{{ currentClient?.name }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getClientStatusType(currentClient?.status)">{{ getClientStatusText(currentClient?.status) }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="Client ID" :span="2">{{ currentClient?.clientId }}</el-descriptions-item>
        <el-descriptions-item label="应用描述" :span="2">{{ currentClient?.description || '-' }}</el-descriptions-item>
        <el-descriptions-item label="回调地址" :span="2">{{ currentClient?.redirectUris }}</el-descriptions-item>
        <el-descriptions-item label="授权范围" :span="2">
          <el-tag v-for="scope in currentClient?.allowedScopes?.split(' ') || []" :key="scope" size="small" style="margin-right: 4px">
            {{ scope }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDate(currentClient?.createdAt) }}</el-descriptions-item>
      </el-descriptions>
      <template #footer v-if="currentClient?.status === 'Pending'">
        <el-button @click="showDetailDialog = false">关闭</el-button>
        <el-button type="success" @click="handleApproveFromDetail">批准</el-button>
        <el-button type="danger" @click="handleRejectFromDetail">拒绝</el-button>
      </template>
    </el-dialog>

    <!-- 拒绝原因输入弹窗 -->
    <el-dialog v-model="showRejectReasonDialog" title="拒绝原因" width="450px" :close-on-click-modal="false">
      <el-form :model="rejectReasonForm" ref="rejectReasonFormRef" label-width="80px">
        <el-form-item label="原因" prop="reason" :rules="[{ required: true, message: '请输入拒绝原因' }]">
          <el-input v-model="rejectReasonForm.reason" type="textarea" :rows="4" placeholder="请输入拒绝原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRejectReasonDialog = false">取消</el-button>
        <el-button type="danger" :loading="rejecting" @click="confirmReject">确认拒绝</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'
import { getClients, approveClient, rejectClient, deleteClient } from '@/api/client'
import api from '@/utils/api'
import { formatDate } from '@/utils/format'
import { getClientStatusType, getClientStatusText } from '@/constants'

const loading = ref(false)
const clients = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const total = ref(0)

const showRegisterDialog = ref(false)
const showDetailDialog = ref(false)
const showRejectReasonDialog = ref(false)
const currentClient = ref<any>(null)
const rejecting = ref(false)
const rejectReasonFormRef = ref()
const rejectReasonForm = ref({ reason: '' })

const searchForm = reactive({
  name: '',
  status: ''
})

const formRef = ref<FormInstance>()
const form = reactive({
  name: '',
  redirectUris: '',
  allowedScopes: 'openid profile email'
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入应用名称', trigger: 'blur' }],
  redirectUris: [{ required: true, message: '请输入回调地址', trigger: 'blur' }]
}

onMounted(async () => {
  await loadClients()
})

const loadClients = async () => {
  loading.value = true
  try {
    const res = await getClients({
      page: currentPage.value,
      pageSize: pageSize.value,
      keyword: searchForm.name
    })
    clients.value = res.data || res
    total.value = res.total || clients.value.length
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  currentPage.value = 1
  loadClients()
}

const handleReset = () => {
  searchForm.name = ''
  searchForm.status = ''
  handleSearch()
}

const handleSizeChange = () => {
  currentPage.value = 1
  loadClients()
}

const handleCurrentChange = () => {
  loadClients()
}

const handleRegister = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      try {
        const res: any = await api({
          url: '/clients/register',
          method: 'post',
          data: {
            name: form.name,
            redirectUris: form.redirectUris,
            allowedScopes: form.allowedScopes
          }
        })
        ElMessage.success(`创建成功，Client ID: ${res.clientId}`)
        showRegisterDialog.value = false
        form.name = ''
        form.redirectUris = ''
        await loadClients()
      } catch (error: any) {
        // 错误已在 api.ts 统一处理
      }
    }
  })
}

const handleApprove = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定批准该客户端吗？', '提示', { confirmButtonText: '确定', cancelButtonText: '取消' })
    await approveClient(row.id)
    ElMessage.success('已批准')
    await loadClients()
  } catch (error: any) {
    if (error !== 'cancel') {
      // 错误已在 api.ts 统一处理
    }
  }
}

const handleReject = (row: any) => {
  currentClient.value = row
  rejectReasonForm.value.reason = ''
  showRejectReasonDialog.value = true
}

const confirmReject = async () => {
  if (!rejectReasonFormRef.value) return
  try {
    await rejectReasonFormRef.value.validate()
  } catch {
    return
  }
  rejecting.value = true
  try {
    await rejectClient(currentClient.value.id, { reason: rejectReasonForm.value.reason })
    ElMessage.success('已拒绝')
    showRejectReasonDialog.value = false
    showDetailDialog.value = false
    await loadClients()
  } catch (error: any) {
    // 错误已在 api.ts 统一处理
  } finally {
    rejecting.value = false
  }
}

const handleView = (row: any) => {
  currentClient.value = row
  showDetailDialog.value = true
}

const handleApproveFromDetail = () => {
  if (currentClient.value) {
    handleApprove(currentClient.value)
  }
}

const handleRejectFromDetail = () => {
  if (currentClient.value) {
    handleReject(currentClient.value)
  }
}

const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定删除该客户端吗？此操作不可恢复', '警告', { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' })
    await deleteClient(row.id)
    ElMessage.success('已删除')
    await loadClients()
  } catch (error: any) {
    if (error !== 'cancel') {
      // 错误已在 api.ts 统一处理
    }
  }
}
</script>

<style scoped>
.clients-page {
  padding: 20px;
}

.page-card {
  border-radius: 4px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.page-title {
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.search-area {
  padding: 0;
}

.search-form {
  margin: 0;
}

.search-form .el-form-item {
  margin-bottom: 0;
}

.pagination-area {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}
</style>
