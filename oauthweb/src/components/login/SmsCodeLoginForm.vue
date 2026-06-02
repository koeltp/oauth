<template>
  <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
    <el-form-item prop="phone">
      <el-input v-model="form.phone" placeholder="手机号" size="large" :prefix-icon="Phone" />
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
import { Phone, Key } from '@element-plus/icons-vue'
import { sendSmsCode, verifyCode } from '@/api/auth'
import { useLogin } from '@/composables/useLogin'
import { ElMessage } from 'element-plus'

const { loading, handleLoginSuccess, startCountdown } = useLogin()

const formRef = ref<FormInstance>()
const form = reactive({
  phone: '',
  code: ''
})
const rules: FormRules = {
  phone: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }]
}

const countdown = ref(0)

async function handleSendCode() {
  if (!form.phone) {
    ElMessage.warning('请输入手机号')
    return
  }
  try {
    await sendSmsCode({ phone: form.phone, purpose: 0 })
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
      const res: any = await verifyCode({ identifier: form.phone, type: 1, code: form.code, purpose: 0 })
      if (res.require2Fa) {
        ElMessage.warning('请使用密码登录并输入两步验证码')
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