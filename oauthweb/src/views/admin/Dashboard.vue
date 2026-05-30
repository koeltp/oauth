<template>
  <div class="dashboard">
    <h1 class="page-title">仪表盘</h1>

    <el-row :gutter="20" class="stat-cards">
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-item">
            <el-icon class="stat-icon" color="#409eff"><Refresh /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.pendingClients }}</span>
              <span class="stat-label">待审核客户端</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-item">
            <el-icon class="stat-icon" color="#67c23a"><Checked  /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.approvedClients }}</span>
              <span class="stat-label">已批准客户端</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-item">
            <el-icon class="stat-icon" color="#e6a23c"><Monitor /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.totalClients }}</span>
              <span class="stat-label">总客户端数</span>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-item">
            <el-icon class="stat-icon" color="#f56c6c"><User /></el-icon>
            <div class="stat-info">
              <span class="stat-value">{{ stats.totalUsers }}</span>
              <span class="stat-label">总用户数</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card class="recent-activity" shadow="never">
      <template #header>
        <span>最近活动</span>
      </template>
      <el-timeline>
        <el-timeline-item timestamp="2024-01-15 10:30" placement="top">
          <p>用户 admin 批准了客户端 "MyApp"</p>
        </el-timeline-item>
        <el-timeline-item timestamp="2024-01-15 09:15" placement="top">
          <p>新用户 test@example.com 注册</p>
        </el-timeline-item>
        <el-timeline-item timestamp="2024-01-14 16:45" placement="top">
          <p>客户端 "DemoApp" 提交审核</p>
        </el-timeline-item>
      </el-timeline>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Monitor, Checked , Refresh, User } from '@element-plus/icons-vue'
import adminApi from '@/utils/admin-api'

const stats = ref({
  pendingClients: 0,
  approvedClients: 0,
  totalClients: 0,
  totalUsers: 0
})

onMounted(async () => {
  try {
    const res: any = await adminApi.get('/admin/dashboard')
    stats.value = res
  } catch (error) {
    console.error('Failed to load dashboard')
  }
})
</script>

<style scoped>
.dashboard {
  padding: 20px;
}

.page-title {
  font-size: 24px;
  margin-bottom: 30px;
  color: #333;
}
.recent-activity {
  margin-top: 40px;
}
</style>