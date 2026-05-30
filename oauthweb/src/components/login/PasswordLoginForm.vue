<template>
  <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
    <el-form-item prop="email">
      <el-input v-model="form.email" placeholder="邮箱" size="large" :prefix-icon="Message" />
    </el-form-item>
    <el-form-item prop="password">
      <el-input v-model="form.password" type="password" placeholder="密码" size="large" :prefix-icon="Lock" show-password />
    </el-form-item>
    <el-form-item v-if="showTwoFaField" prop="twoFaCode">
      <el-input v-model="form.twoFaCode" placeholder="两步验证码" size="large" :prefix-icon="Key" />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" size="large" style="width: 100%" :loading="loading" @click="handleSubmit">
        登录
      </el-button>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { Message, Lock, Key } from '@element-plus/icons-vue'
import { loginWithPassword } from '@/api/auth'
import { useLogin } from '@/composables/useLogin'

const emit = defineEmits<{
  (e: 'twoFaRequired', userId: string): void
}>()

const { loading, handleLoginSuccess } = useLogin()

const formRef = ref<FormInstance>()
const form = reactive({
  email: '',
  password: '',
  twoFaCode: ''
})
const rules: FormRules = {
  email: [{ required: true, message: '请输入邮箱', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const showTwoFaField = ref(false)

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      const res: any = await loginWithPassword(form)
      if (res.require2Fa) {
        showTwoFaField.value = true
        emit('twoFaRequired', res.userId)
      } else {
        handleLoginSuccess(res)
      }
    } catch {
    } finally {
      loading.value = false
    }
  })
}
</script>