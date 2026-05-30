<template>
  <div class="users-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">用户管理</span>
        </div>
      </template>
      
      <!-- 搜索区域 -->
      <div class="search-area">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="用户名">
            <el-input v-model="searchForm.username" placeholder="请输入用户名" clearable />
          </el-form-item>
          <el-form-item label="邮箱">
            <el-input v-model="searchForm.email" placeholder="请输入邮箱" clearable />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 150px">
              <el-option label="正常" value="Active" />
              <el-option label="未激活" value="Inactive" />
              <el-option label="已禁用" value="Banned" />
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
      <el-table :data="users" v-loading="loading" stripe style="margin-top: 20px">
        <el-table-column type="index" label="序号" width="80" align="center" />
        <el-table-column prop="username" label="用户名" min-width="120" />
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column prop="phone" label="手机号" width="140">
          <template #default="{ row }">
            {{ row.phone || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="emailVerified" label="邮箱验证" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.emailVerified ? 'success' : 'info'" size="small">
              {{ row.emailVerified ? '已验证' : '未验证' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="twoFactorEnabled" label="2FA" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.twoFactorEnabled ? 'success' : 'info'" size="small">
              {{ row.twoFactorEnabled ? '已开启' : '未开启' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="注册时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleView(row)">查看</el-button>
            <el-button type="danger" link size="small" @click="handleBan(row)" v-if="row.status === 'Active'">
              禁用
            </el-button>
            <el-button type="success" link size="small" @click="handleUnban(row)" v-else>
              启用
            </el-button>
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search } from '@element-plus/icons-vue'
import { getUsers, updateUserStatus } from '@/api/user'
import { formatDate } from '@/utils/format'

const loading = ref(false)
const users = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(10)
const total = ref(0)

const searchForm = reactive({
  username: '',
  email: '',
  status: ''
})

onMounted(async () => {
  await loadUsers()
})

const loadUsers = async () => {
  loading.value = true
  try {
    const res = await getUsers({ 
      page: currentPage.value, 
      pageSize: pageSize.value,
      keyword: searchForm.username || searchForm.email
    })
    users.value = res.data
    total.value = res.total
  } finally {
    loading.value = false
  }
}

const getStatusType = (status: string) => {
  const map: Record<string, any> = {
    'Active': 'success',
    'Inactive': 'info',
    'Banned': 'danger'
  }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = {
    'Active': '正常',
    'Inactive': '未激活',
    'Banned': '已禁用'
  }
  return map[status] || status
}

const handleSearch = () => {
  currentPage.value = 1
  loadUsers()
}

const handleReset = () => {
  searchForm.username = ''
  searchForm.email = ''
  searchForm.status = ''
  handleSearch()
}

const handleSizeChange = () => {
  currentPage.value = 1
  loadUsers()
}

const handleCurrentChange = () => {
  loadUsers()
}

const handleView = (row: any) => {
  ElMessageBox.alert(`
    <div style="text-align: left;">
      <p><strong>用户名：</strong>${row.username}</p>
      <p><strong>邮箱：</strong>${row.email}</p>
      <p><strong>手机号：</strong>${row.phone || '-'}</p>
      <p><strong>邮箱验证：</strong>${row.emailVerified ? '已验证' : '未验证'}</p>
      <p><strong>2FA：</strong>${row.twoFactorEnabled ? '已开启' : '未开启'}</p>
      <p><strong>状态：</strong>${getStatusText(row.status)}</p>
      <p><strong>注册时间：</strong>${formatDate(row.createdAt)}</p>
    </div>
  `, '用户详情', {
    dangerouslyUseHTMLString: true,
    confirmButtonText: '关闭'
  })
}

const handleBan = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定禁用该用户吗？', '警告', { type: 'warning' })
    await updateUserStatus(row.id, 'Banned')
    ElMessage.success('已禁用')
    await loadUsers()
  } catch (error: any) {
    if (error !== 'cancel') {
      // 错误已在 api.ts 统一处理
    }
  }
}

const handleUnban = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定启用该用户吗？', '提示')
    await updateUserStatus(row.id, 'Active')
    ElMessage.success('已启用')
    await loadUsers()
  } catch (error: any) {
    if (error !== 'cancel') {
      // 错误已在 api.ts 统一处理
    }
  }
}
</script>

<style scoped>
.users-page {
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
