import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/services/api'
import type { PaginatedResponse, User } from '@/types'

export const useUserStore = defineStore('users', () => {
  const items = ref<User[]>([])
  const total = ref(0)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchAll = async (page = 1, pageSize = 10) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<PaginatedResponse<User>>('/users', {
        params: { page, pageSize },
      })
      items.value = response.data.items
      total.value = response.data.total
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not load users'
    } finally {
      loading.value = false
    }
  }

  const create = async (email: string, password: string, role: string, nickname: string) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<User>('/users', { email, password, role, nickname, isAdmin: role === 'ADMIN' })
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not create user'
      throw new Error(error.value ?? 'User error')
    } finally {
      loading.value = false
    }
  }

  const update = async (id: number, payload: Partial<User> & { password?: string }) => {
    loading.value = true
    error.value = null
    try {
      const mappedPayload: any = {
        email: payload.email,
        nickname: payload.nickname,
        role: payload.role,
        isAdmin: payload.role === 'ADMIN',
      }
      if (payload.password) mappedPayload.passwordHash = payload.password
      const response = await api.put<User>(`/users/${id}`, mappedPayload)
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not update user'
      throw new Error(error.value ?? 'User error')
    } finally {
      loading.value = false
    }
  }

  const remove = async (id: number) => {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/users/${id}`)
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not delete user'
      throw new Error(error.value ?? 'User error')
    } finally {
      loading.value = false
    }
  }

  return {
    items,
    total,
    loading,
    error,
    fetchAll,
    create,
    update,
    remove,
  }
})
