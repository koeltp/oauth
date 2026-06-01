import api from '@/utils/api'
import type { SearchPager, PagedResult } from './types'

export interface AdminDto {
  id: string
  username: string
  email: string | null
  role: string
  status: string
  createdAt: string
  lastLoginAt: string | null
}

export const getAdmins = (query?: SearchPager): Promise<PagedResult<AdminDto>> => {
  return api({
    url: '/admin/admins/list',
    method: 'post',
    data: query || {}
  })
}

export const createAdmin = (data: { username: string; password: string; role?: string }): Promise<{ id: string; username: string; role: string }> => {
  return api({
    url: '/admin/admins',
    method: 'post',
    data
  })
}

export const updateAdmin = (id: string, data: { email?: string; password?: string; role?: string }): Promise<AdminDto> => {
  return api({
    url: `/admin/admins/${id}`,
    method: 'put',
    data
  })
}

export const deleteAdmin = (id: string): Promise<void> => {
  return api({
    url: `/admin/admins/${id}`,
    method: 'delete'
  })
}

export const updateAdminStatus = (id: string, status: string): Promise<AdminDto> => {
  return api({
    url: `/admin/admins/${id}/status`,
    method: 'put',
    data: { status }
  })
}