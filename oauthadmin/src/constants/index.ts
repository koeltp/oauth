// ============ 角色 ============

export const ROLE_MAP: Record<string, string> = {
  SuperAdmin: '超级管理员',
  Admin: '管理员',
  Operator: '操作员'
}

export const ROLE_TYPE_MAP: Record<string, string> = {
  SuperAdmin: 'danger',
  Admin: 'warning',
  Operator: 'info'
}

export const getRoleText = (role: string) => ROLE_MAP[role] || role
export const getRoleType = (role: string) => ROLE_TYPE_MAP[role] || 'info'

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

// ============ 通用状态（用户/管理员共用） ============

const STATUS_TYPE_MAP: Record<string, string> = {
  Active: 'success',
  Inactive: 'info',
  Banned: 'danger'
}

const STATUS_TEXT_MAP: Record<string, string> = {
  Active: '正常',
  Inactive: '未激活',
  Banned: '已禁用'
}

const getStatusType = (status: string) => STATUS_TYPE_MAP[status] || 'info'
const getStatusText = (status: string) => STATUS_TEXT_MAP[status] || status

// 用户状态 — 直接引用通用映射
export const USER_STATUS_TYPE_MAP = STATUS_TYPE_MAP
export const USER_STATUS_TEXT_MAP = STATUS_TEXT_MAP
export const getUserStatusType = getStatusType
export const getUserStatusText = getStatusText

// 管理员状态 — 直接引用通用映射
export const ADMIN_STATUS_TYPE_MAP = STATUS_TYPE_MAP
export const ADMIN_STATUS_TEXT_MAP = STATUS_TEXT_MAP
export const getAdminStatusType = getStatusType
export const getAdminStatusText = getStatusText