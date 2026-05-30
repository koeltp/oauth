<template>
  <UserLayout>
    <div class="client-register-container">
      <el-card class="register-card">
        <template #header>
          <div class="card-header">
            <span>{{ isEditMode ? '编辑应用' : '注册 OAuth 客户端' }}</span>
          </div>
        </template>

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          label-position="top"
          class="register-form"
        >
          <el-form-item label="应用名称" prop="name">
            <el-input v-model="form.name" placeholder="输入应用名称" size="large" />
          </el-form-item>

          <el-form-item label="应用描述" prop="description">
            <el-input
              v-model="form.description"
              type="textarea"
              :rows="3"
              placeholder="简要描述您的应用功能"
            />
          </el-form-item>

          <el-form-item label="回调地址" prop="redirectUris">
            <el-input
              v-model="form.redirectUris"
              type="textarea"
              :rows="2"
              placeholder="输入回调地址，多个地址用换行分隔"
            />
            <div class="form-tip">用户授权后将被重定向到此处填写的 URL</div>
          </el-form-item>

          <el-form-item label="申请权限" prop="scopes">
            <el-checkbox-group v-model="form.scopes">
              <el-checkbox value="openid">OpenID Connect 身份验证</el-checkbox>
              <el-checkbox value="profile">获取用户基本信息</el-checkbox>
              <el-checkbox value="email">获取用户邮箱</el-checkbox>
              <el-checkbox value="phone">获取用户手机号</el-checkbox>
            </el-checkbox-group>
          </el-form-item>

          <el-form-item>
            <el-button type="primary" size="large" style="width: 100%" @click="handleSubmit" :loading="loading">
              {{ isEditMode ? '保存修改' : '提交申请' }}
            </el-button>
          </el-form-item>
        </el-form>
      </el-card>
    </div>
  </UserLayout>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import UserLayout from '@/layouts/UserLayout.vue'
import api from '@/utils/api'

const route = useRoute()
const router = useRouter()

const isEditMode = computed(() => !!route.query.edit)

const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({
  name: '',
  description: '',
  redirectUris: '',
  scopes: ['openid'] as string[]
})

const validateRedirectUris = (_rule: any, value: string, callback: any) => {
  if (!value) {
    callback(new Error('请输入回调地址'))
    return
  }
  const uris = value.split('\n').filter((uri: string) => uri.trim())
  if (uris.length === 0) {
    callback(new Error('请输入回调地址'))
    return
  }
  for (const uri of uris) {
    const trimmed = uri.trim()
    if (trimmed.startsWith('/')) {
      continue
    }
    try {
      const url = new URL(trimmed)
      if (url.protocol !== 'http:' && url.protocol !== 'https:') {
        callback(new Error(`无效的协议: ${trimmed}`))
        return
      }
      if (!url.hostname.includes('.') && url.hostname !== 'localhost') {
        callback(new Error(`无效的域名: ${trimmed}`))
        return
      }
    } catch {
      callback(new Error(`无效的回调地址: ${trimmed}`))
      return
    }
  }
  callback()
}

const rules: FormRules = {
  name: [{ required: true, message: '请输入应用名称', trigger: 'blur' }],
  redirectUris: [{ required: true, message: '请输入回调地址', trigger: 'blur' },
    { validator: validateRedirectUris, trigger: 'blur' }
  ],
  scopes: [{ type: 'array', required: true, min: 1, message: '请至少选择一个权限', trigger: 'change' }]
}

onMounted(async () => {
  if (isEditMode.value) {
    loading.value = true
    try {
      const clientId = route.query.edit as string
      const res: any = await api.get(`/clients/${clientId}`)
      if (res) {
        form.name = res.name || ''
        form.description = res.description || ''
        form.redirectUris = res.redirectUris || ''
        form.scopes = (res.allowedScopes || '').split(' ').filter(Boolean)
      }
    } catch {
      ElMessage.error('加载应用信息失败')
      router.push('/apps')
    } finally {
      loading.value = false
    }
  }
})

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      const payload = {
        name: form.name,
        description: form.description || null,
        redirectUris: form.redirectUris,
        allowedScopes: form.scopes.join(' ')
      }

      if (isEditMode.value) {
        await api.put(`/clients/${route.query.edit}`, payload)
        ElMessage.success('保存成功')
        router.push('/apps')
      } else {
        await api.post('/clients/register', payload)
        ElMessage.success('申请提交成功')
        router.push('/apps')
      }
    } catch (error: any) {
      ElMessage.error(error.response?.data?.message || (isEditMode.value ? '保存失败' : '提交失败'))
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped>
.client-register-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px 0;
}

.register-card {
  border-radius: 8px;
}

.card-header span {
  font-size: 16px;
  font-weight: 600;
}

.register-form {
  margin-top: 20px;
}

.form-tip {
  font-size: 12px;
  color: #999;
  margin-top: 4px;
}
</style>