<template>
  <div class="admins-page">
    <el-card class="page-card">
      <template #header>
        <div class="card-header">
          <span class="page-title">管理员管理</span>
          <el-button type="primary" @click="handleCreate">
            <el-icon><Plus /></el-icon>
            新增管理员
          </el-button>
        </div>
      </template>

      <div class="search-area">
        <el-form :model="searchForm" inline class="search-form" @keyup.enter="handleSearch">
          <el-form-item>
            <el-input v-model="searchForm.keyword" placeholder="搜索用户名/邮箱" clearable />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">搜索</el-button>
            <el-button @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table :data="admins" v-loading="loading" stripe>
        <el-table-column type="index" label="序号" width="80" align="center" />
        <el-table-column prop="username" label="用户名" min-width="120" />
        <el-table-column prop="email" label="邮箱" min-width="180">
          <template #default="{ row }">
            {{ row.email || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="role" label="角色" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="getRoleType(row.role)" size="small">
              {{ getRoleText(row.role) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getAdminStatusType(row.status)" size="small">
              {{ getAdminStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="最后登录" width="180">
          <template #default="{ row }">
            {{ row.lastLoginAt ? formatDate(row.lastLoginAt) : '从未登录' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button
              :type="row.status === 'Active' ? 'danger' : 'success'"
              link
              size="small"
              @click="handleToggleStatus(row)"
            >
              {{ row.status === 'Active' ? '禁用' : '启用' }}
            </el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)" v-if="row.role !== 'SuperAdmin'">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

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

    <!-- 创建/编辑管理员弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEditing ? '编辑管理员' : '新增管理员'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" :disabled="isEditing" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item v-if="!isEditing" label="密码" prop="password">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-select v-model="form.role" style="width: 100%">
            <el-option label="超级管理员" value="SuperAdmin" />
            <el-option label="管理员" value="Admin" />
            <el-option label="操作员" value="Operator" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { getAdmins, createAdmin, updateAdmin, deleteAdmin, updateAdminStatus } from '@/api/admins'
import type { AdminDto } from '@/api/admins'
import { formatDate } from '@/utils/format'
import { getRoleText, getRoleType, getAdminStatusType, getAdminStatusText } from '@/constants'

const loading = ref(false)
const admins = ref<AdminDto[]>([])

const currentPage = ref(1)
const pageSize = ref(10)
const total = ref(0)

const searchForm = ref({
  keyword: ''
})

const dialogVisible = ref(false)
const isEditing = ref(false)
const submitting = ref(false)
const editingId = ref<string>('')
const formRef = ref<FormInstance>()

const form = ref({
  username: '',
  email: '',
  password: '',
  role: 'Operator'
})

const rules = computed<FormRules>(() => {
  const baseRules: FormRules = {
    username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
    role: [{ required: true, message: '请选择角色', trigger: 'change' }]
  }
  if (!isEditing.value) {
    baseRules.password = [
      { required: true, message: '请输入密码', trigger: 'blur' },
      { min: 8, message: '密码长度不能少于8位', trigger: 'blur' }
    ]
  }
  return baseRules
})

onMounted(async () => {
  await loadAdmins()
})

const loadAdmins = async () => {
  loading.value = true
  try {
    const res = await getAdmins({
      page: currentPage.value,
      pageSize: pageSize.value,
      keyword: searchForm.value.keyword || undefined
    })
    admins.value = res.data
    total.value = res.total
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  currentPage.value = 1
  loadAdmins()
}

const handleReset = () => {
  searchForm.value.keyword = ''
  handleSearch()
}

const handleSizeChange = () => {
  currentPage.value = 1
  loadAdmins()
}

const handleCurrentChange = () => {
  loadAdmins()
}

const handleCreate = () => {
  isEditing.value = false
  editingId.value = ''
  form.value = { username: '', email: '', password: '', role: 'Operator' }
  dialogVisible.value = true
}

const handleEdit = (row: AdminDto) => {
  isEditing.value = true
  editingId.value = row.id
  form.value = {
    username: row.username,
    email: row.email || '',
    password: '',
    role: row.role
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitting.value = true
    try {
      if (isEditing.value) {
        const data: any = {}
        if (form.value.email) data.email = form.value.email
        data.role = form.value.role
        await updateAdmin(editingId.value, data)
        ElMessage.success('管理员已更新')
      } else {
        await createAdmin({
          username: form.value.username,
          password: form.value.password,
          role: form.value.role
        })
        ElMessage.success('管理员已创建')
      }
      dialogVisible.value = false
      await loadAdmins()
    } finally {
      submitting.value = false
    }
  })
}

const handleDelete = async (row: AdminDto) => {
  try {
    await ElMessageBox.confirm(`确定删除管理员 "${row.username}" 吗？`, '警告', { type: 'warning' })
    await deleteAdmin(row.id)
    ElMessage.success('管理员已删除')
    await loadAdmins()
  } catch (error: any) {
    if (error !== 'cancel') {
      // 错误已在 api.ts 统一处理
    }
  }
}

const handleToggleStatus = async (row: AdminDto) => {
  const newStatus = row.status === 'Active' ? 'Inactive' : 'Active'
  const actionText = newStatus === 'Inactive' ? '禁用' : '启用'
  try {
    await ElMessageBox.confirm(`确定${actionText}管理员 "${row.username}" 吗？`, '提示')
    await updateAdminStatus(row.id, newStatus)
    ElMessage.success(`管理员已${actionText}`)
    await loadAdmins()
  } catch (error: any) {
    if (error !== 'cancel') {
      // 错误已在 api.ts 统一处理
    }
  }
}
</script>

<style scoped>
.admins-page {
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
  padding: 0 0 16px;
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