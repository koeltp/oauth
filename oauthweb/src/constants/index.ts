export const CLIENT_STATUS_TYPE_MAP: Record<string, string> = {
  Draft: 'info',
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