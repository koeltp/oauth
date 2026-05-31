import api from '@/utils/api'

export const getUsers = (data?: { page?: number; pageSize?: number; keyword?: string }) => {
  return api({
    url: '/admin/users',
    method: 'post',
    data
  })
}

export const getUserById = (id: string) => {
  return api({
    url: `/admin/users/${id}`,
    method: 'get'
  })
}

export const updateUser = (id: string, data: { email?: string; username?: string }) => {
  return api({
    url: `/admin/users/${id}`,
    method: 'put',
    data
  })
}

export const deleteUser = (id: string) => {
  return api({
    url: `/admin/users/${id}`,
    method: 'delete'
  })
}

export const updateUserStatus = (id: string, status: string) => {
  return api({
    url: `/admin/users/${id}/status`,
    method: 'put',
    data: { status }
  })
}
