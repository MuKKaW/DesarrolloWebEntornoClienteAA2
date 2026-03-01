import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import api from '@/services/api'
import type { DashboardStats, Product } from '@/types'

interface BidsByDatePoint {
  date: string
  count: number
}

interface TopProductPoint {
  id: number
  title: string
  bidCount: number
  currentPrice: number
}

export const useDashboardStore = defineStore('dashboard', () => {
  const stats = ref<DashboardStats>({
    totalProducts: 0,
    totalBids: 0,
    avgPrice: 0,
    activeProducts: 0,
  })
  const bidsByDate = ref<BidsByDatePoint[]>([])
  const topProducts = ref<TopProductPoint[]>([])
  const statusDistribution = ref<Record<string, number>>({ DRAFT: 0, ACTIVE: 0, ENDED: 0 })
  const loading = ref(false)
  const error = ref<string | null>(null)

  const kpis = computed(() => stats.value)

  const load = async (startDate?: string, endDate?: string) => {
    loading.value = true
    error.value = null
    try {
      const [statsRes, bidsDateRes, topRes, productsRes] = await Promise.all([
        api.get('/statistics'),
        api.get('/statistics/bids-by-date'),
        api.get('/statistics/top-products'),
        api.get('/products', { params: { page: 1, pageSize: 200, startDate, endDate } }),
      ])

      stats.value = statsRes.data
      bidsByDate.value = bidsDateRes.data
      topProducts.value = topRes.data

      const products = (productsRes.data.items ?? []) as Product[]
      statusDistribution.value = products.reduce(
        (acc, product) => {
          acc[product.status] = (acc[product.status] ?? 0) + 1
          return acc
        },
        { DRAFT: 0, ACTIVE: 0, ENDED: 0 } as Record<string, number>,
      )
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Could not load dashboard'
    } finally {
      loading.value = false
    }
  }

  return {
    stats,
    bidsByDate,
    topProducts,
    statusDistribution,
    loading,
    error,
    kpis,
    load,
  }
})