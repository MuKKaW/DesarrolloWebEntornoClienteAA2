<template>
  <section class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold">Panel de administracion</h1>
        <p class="text-sm text-slate-500">Cancela subastas activas cuando sea necesario.</p>
      </div>
      <el-button :loading="productStore.loading" @click="load">Recargar</el-button>
    </div>

    <el-card>
      <el-table :data="productStore.items" stripe border>
        <el-table-column prop="title" label="Subasta" min-width="220" />
        <el-table-column label="Precio actual" width="150">
          <template #default="scope">{{ scope.row.currentPrice.toFixed(2) }} EUR</template>
        </el-table-column>
        <el-table-column prop="startsAt" label="Inicio" width="200" />
        <el-table-column prop="endsAt" label="Fin" width="200" />
        <el-table-column prop="status" label="Estado" width="120" />
        <el-table-column label="Acciones" width="160">
          <template #default="scope">
            <el-button
              size="small"
              type="danger"
              :loading="cancellingId === scope.row.id"
              @click="confirmCancel(scope.row)"
            >
              Cancelar
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useProductStore } from '@/stores/products'
import type { Product } from '@/types'

const productStore = useProductStore()
const cancellingId = ref<number | null>(null)

onMounted(load)

function load() {
  productStore.fetchAll({ page: 1, pageSize: 50, status: 'ACTIVE' })
}

async function confirmCancel(product: Product) {
  try {
    await ElMessageBox.confirm(
      `Se cancelara la subasta "${product.title}" y dejara de estar activa.`,
      'Confirmacion',
      { type: 'warning' },
    )
    cancellingId.value = product.id
    await productStore.cancelAuction(product.id)
    ElMessage.success('Subasta cancelada')
    load()
  } catch (error: any) {
    if (error?.message) {
      ElMessage.error(error.message)
    }
  } finally {
    cancellingId.value = null
  }
}
</script>
