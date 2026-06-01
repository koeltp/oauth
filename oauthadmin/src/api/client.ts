import api from '@/utils/api'
import type { SearchPager } from './types'

export const getClients = (data?: SearchPager<{ name?: string; status?: string }>) => {
  return api({
    url: '/admin/clients',
    method: 'post',
    data
  })
}

export const getClientById = (id: string) => {
  return api({
    url: `/admin/clients/${id}`,
    method: 'get'
  })
}

export const createClient = (data: { name: string; description?: string; redirectUri: string }) => {
  return api({
    url: '/admin/clients',
    method: 'post',
    data
  })
}

export const updateClient = (id: string, data: { name?: string; description?: string; redirectUri?: string }) => {
  return api({
    url: `/admin/clients/${id}`,
    method: 'put',
    data
  })
}

export const deleteClient = (id: string) => {
  return api({
    url: `/admin/clients/${id}`,
    method: 'delete'
  })
}

export const approveClient = (id: string) => {
  return api({
    url: `/admin/clients/${id}/approve`,
    method: 'put'
  })
}

export const rejectClient = (id: string, data?: { reason: string }) => {
  return api({
    url: `/admin/clients/${id}/reject`,
    method: 'put',
    data
  })
}
