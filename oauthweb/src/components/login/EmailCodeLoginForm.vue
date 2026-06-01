<template>
  <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
    <el-form-item prop="email">
      <el-input v-model="form.email" placeholder="邮箱" size="large" :prefix-icon="Message" />
    </el-form-item>
    <el-form-item prop="code">
      <el-input v-model="form.code" placeholder="验证码" size="large" :prefix-icon="Key">
        <template #append>
          <el-button :disabled="countdown > 0" @click="handleSendCode">
            {{ countdown > 0 ? `${countdown}s` : '发送验证码' }}
          </el-button>
        </template>
      </el-input>
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
import { Message, Key } from '@element-plus/icons-vue'
import { sendEmailCode, verifyCode } from '@/api/auth'
import { useLogin } from '@/composables/useLogin'
import { ElMessage } from 'element-plus'

const { loading, handleLoginSuccess, startCountdown } = useLogin()

const formRef = ref<FormInstance>()
const form = reactive({
  email: '',
  code: ''
})
const rules: FormRules = {
  email: [{ required: true, message: '请输入邮箱', trigger: 'blur' }],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }]
}

const countdown = ref(0)

async function handleSendCode() {
  if (!form.email) {
    ElMessage.warning('请输入邮箱')
    return
  }
  try {
    await sendEmailCode({ email: form.email, purpose: 0 })
    ElMessage.success('验证码已发送')
    startCountdown(countdown)
  } catch {
    ElMessage.error('发送失败')
  }
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      const res: any = await verifyCode({ identifier: form.email, type: 0, code: form.code, purpose: 0 })
      if (res.require2Fa) {
        ElMessage.warning('请使用密码登录并输入两步验证码')
      } else if (res.verified) {
        handleLoginSuccess(res)
      }
    } catch {
    } finally {
      loading.value = false
    }
  })
}
</script>