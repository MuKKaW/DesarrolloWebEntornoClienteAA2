<template>
  <section class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold">Gestion de productos</h1>
      <el-button type="primary" :disabled="!canCreate" @click="openCreate">Nuevo producto</el-button>
    </div>

    <el-card>
      <div class="grid grid-cols-1 gap-3 md:grid-cols-5">
        <el-input v-model="filters.search" placeholder="Buscar" />
        <el-select v-model="filters.sort" style="width: 100%">
          <el-option label="Recientes" value="createdAtDesc" />
          <el-option label="Precio asc" value="priceAsc" />
          <el-option label="Precio desc" value="priceDesc" />
        </el-select>
        <el-input v-model.number="filters.minPrice" type="number" placeholder="Precio min" />
        <el-input v-model.number="filters.maxPrice" type="number" placeholder="Precio max" />
        <el-button @click="load">Filtrar</el-button>
      </div>
    </el-card>

    <ProductTable :products="productStore.items" @edit="openEdit" @delete="confirmDelete" />

    <div class="flex justify-center">
      <el-pagination
        layout="prev, pager, next"
        :total="productStore.total"
        :page-size="filters.pageSize"
        :current-page="filters.page"
        @current-change="changePage"
      />
    </div>

    <el-dialog v-model="dialogOpen" width="720px" :title="editing ? 'Editar producto' : 'Crear producto'">
      <ProductForm
        :model-value="editing"
        :is-loading="productStore.loading"
        @submit="submitForm"
        @cancel="dialogOpen = false"
      />
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import ProductForm from '@/components/forms/ProductForm.vue'
import ProductTable from '@/components/ProductTable.vue'
import { useAuthStore } from '@/stores/auth'
import { useProductStore } from '@/stores/products'
import type { Product } from '@/types'

const authStore = useAuthStore()
const productStore = useProductStore()

const dialogOpen = ref(false)
const editing = ref<Product | null>(null)
const filters = reactive({
  page: 1,
  pageSize: 10,
  search: '',
  sort: 'createdAtDesc',
  minPrice: undefined as number | undefined,
  maxPrice: undefined as number | undefined,
})

const canCreate = computed(() => authStore.userRole === 'ADMIN' || authStore.userRole === 'MANAGER')

onMounted(load)

function load() {
  productStore.fetchAll({ ...filters })
}

function changePage(page: number) {
  filters.page = page
  load()
}

function openCreate() {
  editing.value = null
  dialogOpen.value = true
}

function openEdit(product: Product) {
  editing.value = product
  dialogOpen.value = true
}

async function submitForm(values: any) {
  const payload = {
    ...values,
    startsAt: new Date(values.startsAt).toISOString(),
    endsAt: new Date(values.endsAt).toISOString(),
  }

  try {
    if (editing.value) {
      await productStore.update(editing.value.id, payload)
      ElMessage.success('Producto actualizado')
    } else {
      await productStore.create(payload)
      ElMessage.success('Producto creado')
    }
    dialogOpen.value = false
    load()
  } catch (error: any) {
    ElMessage.error(error.message)
  }
}

async function confirmDelete(product: Product) {
  try {
    await ElMessageBox.confirm('Se eliminara el producto seleccionado', 'Confirmacion', { type: 'warning' })
    await productStore.remove(product.id)
    ElMessage.success('Producto eliminado')
    load()
  } catch {
  }
}
</script>