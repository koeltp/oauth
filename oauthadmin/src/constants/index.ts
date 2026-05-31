// ============ 角色 ============

export const ROLE_MAP: Record<string, string> = {
  SuperAdmin: '超级管理员',
  Admin: '管理员',
  Operator: '操作员'
}

export const getRoleText = (role: string) => ROLE_MAP[role] || role

// ============ 管理后台 - 客户端状态 ============

export const CLIENT_STATUS_TYPE_MAP: Record<string, string> = {
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'danger'
}

export const CLIENT_STATUS_TEXT_MAP: Record<string, string> = {
  Draft: '草稿',
  Pending: '待审核',
  Approved: '已批准',
  Rejected: '已拒绝'
}

export const getClientStatusType = (status: string) => CLIENT_STATUS_TYPE_MAP[status] || 'info'
export const getClientStatusText = (status: string) => CLIENT_STATUS_TEXT_MAP[status] || status

// ============ 用户状态 ============

export const USER_STATUS_TYPE_MAP: Record<string, string> = {
  Active: 'success',
  Inactive: 'info',
  Banned: 'danger'
}

export const USER_STATUS_TEXT_MAP: Record<string, string> = {
  Active: '正常',
  Inactive: '未激活',
  Banned: '已禁用'
}

export const getUserStatusType = (status: string) => USER_STATUS_TYPE_MAP[status] || 'info'
export const getUserStatusText = (status: string) => USER_STATUS_TEXT_MAP[status] || status