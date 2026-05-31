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

    <el-card class="recent-activity" shadow="never" v-loading="loading">
      <template #header>
        <span>最近活动</span>
      </template>
      <el-timeline>
        <el-timeline-item
          v-for="activity in activities"
          :key="activity.createdAt + activity.description"
          :timestamp="formatDate(activity.createdAt)"
          placement="top"
        >
          <p>{{ activity.description }}</p>
        </el-timeline-item>
        <el-empty v-if="activities.length === 0 && !loading" description="暂无活动" />
      </el-timeline>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Monitor, Checked , Refresh, User } from '@element-plus/icons-vue'
import api from '@/utils/api'
import { formatDate } from '@/utils/format'

const loading = ref(false)

const stats = ref({
  pendingClients: 0,
  approvedClients: 0,
  totalClients: 0,
  totalUsers: 0
})

interface RecentActivity {
  action: string
  description: string
  adminName: string
  createdAt: string
}

const activities = ref<RecentActivity[]>([])

onMounted(async () => {
  loading.value = true
  try {
    const res: any = await api.get('/admin/dashboard')
    stats.value = res

    const activityRes: any = await api.get('/admin/dashboard/recent-activities')
    activities.value = activityRes || []
  } catch (error) {
    console.error('Failed to load dashboard')
  } finally {
    loading.value = false
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