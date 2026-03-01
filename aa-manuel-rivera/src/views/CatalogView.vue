<template>
  <section class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-3xl font-bold">{{ $t('common.catalog') }}</h1>
      <el-button :loading="productStore.loading" @click="search">Recargar</el-button>
    </div>

    <el-card>
      <div class="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-6">
        <el-input v-model="filters.search" placeholder="Buscar por titulo o descripcion" />
        <el-select v-model="filters.sort" placeholder="Orden" style="width: 100%">
          <el-option label="Mas recientes" value="createdAtDesc" />
          <el-option label="Precio asc" value="priceAsc" />
          <el-option label="Precio desc" value="priceDesc" />
        </el-select>
        <el-input v-model.number="filters.minPrice" type="number" placeholder="Precio min" />
        <el-input v-model.number="filters.maxPrice" type="number" placeholder="Precio max" />
        <el-input v-model="filters.startDate" type="date" placeholder="Fecha inicio" />
        <el-input v-model="filters.endDate" type="date" placeholder="Fecha fin" />
      </div>
      <div class="mt-3 flex gap-2">
        <el-button type="primary" @click="search">Buscar</el-button>
        <el-button @click="resetFilters">Limpiar</el-button>
      </div>
    </el-card>

    <el-skeleton :rows="4" animated v-if="productStore.loading" />

    <template v-else>
      <ProductList v-if="productStore.items.length" :products="productStore.items" @select="openDetail" />
      <el-empty v-else description="No hay resultados" />
    </template>

    <div class="flex justify-center">
      <el-pagination
        layout="prev, pager, next"
        :total="productStore.total"
        :page-size="filters.pageSize"
        :current-page="filters.page"
        @current-change="changePage"
      />
    </div>
  </section>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import ProductList from '@/components/ProductList.vue'
import { useProductStore } from '@/stores/products'
import type { Product } from '@/types'

const router = useRouter()
const productStore = useProductStore()

const filters = reactive({
  page: 1,
  pageSize: 9,
  search: '',
  sort: 'createdAtDesc',
  minPrice: undefined as number | undefined,
  maxPrice: undefined as number | undefined,
  startDate: '',
  endDate: '',
})

onMounted(search)

function search() {
  productStore.fetchAll({ ...filters })
}

function resetFilters() {
  filters.page = 1
  filters.search = ''
  filters.sort = 'createdAtDesc'
  filters.minPrice = undefined
  filters.maxPrice = undefined
  filters.startDate = ''
  filters.endDate = ''
  search()
}

function openDetail(product: Product) {
  router.push(`/products/${product.id}`)
}

function changePage(page: number) {
  filters.page = page
  search()
}
</script>