import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/services/api'
import type { Bid } from '@/types'

export const useBidStore = defineStore('bids', () => {
  const items = ref<Bid[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchByProduct = async (productId: number) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<Bid[]>(`/products/${productId}/bids`)
      items.value = response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not load bids'
    } finally {
      loading.value = false
    }
  }

  const create = async (productId: number, userId: number, amount: number) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<Bid>('/bids', { productId, userId, amount })
      items.value = [response.data, ...items.value.filter((item) => item.id !== response.data.id)]
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not place bid'
      throw new Error(error.value ?? 'Bid error')
    } finally {
      loading.value = false
    }
  }

  return {
    items,
    loading,
    error,
    fetchByProduct,
    create,
  }
})
