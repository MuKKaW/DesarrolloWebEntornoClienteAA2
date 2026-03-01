<template>
  <section class="space-y-12">
    <div class="overflow-hidden rounded-2xl bg-gradient-to-r from-blue-700 to-cyan-500 p-10 text-white">
      <h1 class="text-4xl font-extrabold">Subastas publicas en tiempo real</h1>
      <p class="mt-3 max-w-2xl text-lg text-blue-50">
        Registrate, entra en subastas activas y publica tus propios productos para competir por el mejor precio.
      </p>
      <div class="mt-6 flex flex-wrap gap-3">
        <router-link to="/catalog"><el-button type="primary" size="large">Explorar subastas</el-button></router-link>
        <router-link v-if="!authStore.isAuthenticated" to="/register"><el-button size="large">Crear cuenta</el-button></router-link>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
      <el-card>
        <h3 class="text-2xl font-bold">{{ productStore.total }}</h3>
        <p class="text-sm text-slate-600 dark:text-slate-300">Subastas publicadas</p>
      </el-card>
      <el-card>
        <h3 class="text-2xl font-bold">{{ activeProducts }}</h3>
        <p class="text-sm text-slate-600 dark:text-slate-300">Subastas activas</p>
      </el-card>
      <el-card>
        <h3 class="text-2xl font-bold">{{ averagePrice.toFixed(2) }} EUR</h3>
        <p class="text-sm text-slate-600 dark:text-slate-300">Precio medio actual</p>
      </el-card>
    </div>

    <div>
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-2xl font-bold">Subastas destacadas</h2>
        <router-link class="text-sm font-semibold text-blue-600" to="/catalog">Ver catalogo completo</router-link>
      </div>
      <ProductList :products="featured" @select="goToProduct" />
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import ProductList from '@/components/ProductList.vue'
import { useAuthStore } from '@/stores/auth'
import { useProductStore } from '@/stores/products'
import type { Product } from '@/types'

const router = useRouter()
const authStore = useAuthStore()
const productStore = useProductStore()

const featured = computed(() => productStore.items.slice(0, 3))
const activeProducts = computed(() => productStore.items.filter((p) => p.status === 'ACTIVE').length)
const averagePrice = computed(() => {
  if (!productStore.items.length) return 0
  const total = productStore.items.reduce((acc, item) => acc + Number(item.currentPrice), 0)
  return total / productStore.items.length
})

onMounted(() => {
  productStore.fetchAll({ page: 1, pageSize: 20 })
})

const goToProduct = (product: Product) => {
  router.push(`/products/${product.id}`)
}
</script>
