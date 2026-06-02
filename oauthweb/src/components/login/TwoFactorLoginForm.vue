<template>
  <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
    <el-form-item prop="code" class="otp-form-item">
      <el-input-otp
        v-model="form.code"
        :length="6"
        placeholder="请输入验证码"
      />
    </el-form-item>
    <el-form-item>
      <el-button type="primary" size="large" style="width: 100%" :loading="loading" @click="handleSubmit">
        验证
      </el-button>
    </el-form-item>
    <el-form-item>
      <el-button size="large" style="width: 100%" @click="handleCancel">
        返回
      </el-button>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useLogin } from '@/composables/useLogin'
import request from '@/utils/api'

const props = defineProps<{
  userId: string
}>()

const emit = defineEmits<{
  (e: 'cancel'): void
}>()

const { loading, handleLoginSuccess } = useLogin()

const formRef = ref<FormInstance>()
const form = reactive({ code: '' })
const rules: FormRules = {
  code: [
    { required: true, message: '请输入验证码', trigger: 'blur' },
    { len: 6, message: '验证码为 6 位数字', trigger: 'blur' }
  ]
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      const res: any = await request({
        url: '/auth/2fa/verify',
        method: 'post',
        data: { user_id: props.userId, code: form.code }
      })
      handleLoginSuccess(res)
    } catch {
    } finally {
      loading.value = false
    }
  })
}

function handleCancel() {
  emit('cancel')
}
</script>

<style scoped>
.otp-form-item :deep(.el-form-item__content) {
  justify-content: center;
}
</style>