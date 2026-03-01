import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '@/services/api'
import type { PaginatedResponse, Product, ProductStatus } from '@/types'

export interface ProductFilters {
  page?: number
  pageSize?: number
  search?: string
  sort?: string
  minPrice?: number
  maxPrice?: number
  startDate?: string
  endDate?: string
  status?: ProductStatus
}

export const useProductStore = defineStore('products', () => {
  const items = ref<Product[]>([])
  const selectedItem = ref<Product | null>(null)
  const total = ref(0)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const fetchAll = async (filters: ProductFilters = {}) => {
    loading.value = true
    error.value = null
    try {
      const params: Record<string, string | number> = {
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 10,
        sort: filters.sort ?? 'createdAtDesc',
      }
      if (filters.search) params.search = filters.search
      if (typeof filters.minPrice === 'number') params.minPrice = filters.minPrice
      if (typeof filters.maxPrice === 'number') params.maxPrice = filters.maxPrice
      if (filters.startDate) params.startDate = filters.startDate
      if (filters.endDate) params.endDate = filters.endDate
      if (filters.status) params.status = filters.status

      const response = await api.get<PaginatedResponse<Product>>('/products', { params })
      items.value = response.data.items
      total.value = response.data.total
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not load products'
    } finally {
      loading.value = false
    }
  }

  const fetchById = async (id: number) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.get<Product>(`/products/${id}`)
      selectedItem.value = response.data
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not load product'
      throw new Error(error.value ?? 'Product error')
    } finally {
      loading.value = false
    }
  }

  const create = async (payload: Omit<Product, 'id' | 'createdBy'>) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<Product>('/products', payload)
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not create product'
      throw new Error(error.value ?? 'Product error')
    } finally {
      loading.value = false
    }
  }

  const update = async (id: number, payload: Partial<Product>) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.put<Product>(`/products/${id}`, payload)
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not update product'
      throw new Error(error.value ?? 'Product error')
    } finally {
      loading.value = false
    }
  }

  const remove = async (id: number) => {
    loading.value = true
    error.value = null
    try {
      await api.delete(`/products/${id}`)
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not delete product'
      throw new Error(error.value ?? 'Product error')
    } finally {
      loading.value = false
    }
  }

  const cancelAuction = async (id: number) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.put<Product>(`/admin/products/${id}/cancel`)
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not cancel auction'
      throw new Error(error.value ?? 'Product error')
    } finally {
      loading.value = false
    }
  }

  return {
    items,
    selectedItem,
    total,
    loading,
    error,
    fetchAll,
    fetchById,
    create,
    update,
    remove,
    cancelAuction,
  }
})
