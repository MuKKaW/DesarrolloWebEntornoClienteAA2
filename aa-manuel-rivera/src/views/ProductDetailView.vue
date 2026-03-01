<template>
  <section class="space-y-6">
    <el-button @click="router.back()">{{ $t('common.back') }}</el-button>

    <el-skeleton v-if="productStore.loading" :rows="6" animated />

    <template v-else-if="product">
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <el-card class="lg:col-span-2">
          <h1 class="text-3xl font-bold">{{ product.title }}</h1>
          <p class="mt-2 text-slate-600 dark:text-slate-300">{{ product.description }}</p>

          <div class="mt-6 grid grid-cols-1 gap-4 md:grid-cols-3">
            <el-statistic title="Precio inicial" :value="product.startPrice" suffix="EUR" />
            <el-statistic title="Precio actual" :value="effectiveCurrentPrice" suffix="EUR" />
            <el-statistic title="Fin" :value="new Date(product.endsAt).toLocaleString()" />
          </div>
        </el-card>

        <el-card>
          <h2 class="text-lg font-semibold">Realizar puja</h2>
          <p class="mt-1 text-sm text-slate-500" v-if="!authStore.isAuthenticated">
            Inicia sesion para pujar.
          </p>
          <p class="mt-1 text-sm text-slate-500" v-else-if="isLeadingBidder">
            No puedes volver a pujar mientras sigues en cabeza.
          </p>
          <p class="mt-1 text-sm text-slate-500" v-else>Introduce una cantidad mayor al precio actual.</p>

          <div class="mt-4 space-y-3">
            <el-input v-model.number="bidAmount" type="number" :disabled="!canBid" step="0.01" />
            <el-button type="primary" :disabled="!canBid" :loading="bidStore.loading" @click="placeBid">Pujar</el-button>
            <el-button v-if="!authStore.isAuthenticated" @click="uiStore.openAuthModal('login')">Entrar</el-button>
            <p v-if="bidError" class="text-sm text-red-500">{{ bidError }}</p>
          </div>
        </el-card>
      </div>

      <el-card>
        <template #header>
          <div class="font-semibold">Historial de pujas</div>
        </template>
        <BidTable :bids="bidStore.items" />
      </el-card>
    </template>

    <el-empty v-else description="Producto no encontrado" />
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import BidTable from '@/components/BidTable.vue'
import { useAuthStore } from '@/stores/auth'
import { useBidStore } from '@/stores/bids'
import { useProductStore } from '@/stores/products'
import { useUiStore } from '@/stores/ui'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const productStore = useProductStore()
const bidStore = useBidStore()
const uiStore = useUiStore()

const bidAmount = ref<number>(0)
const bidError = ref('')
const product = computed(() => productStore.selectedItem)
const highestBid = computed(() => bidStore.items[0] ?? null)
const effectiveCurrentPrice = computed(() => Math.max(Number(product.value?.currentPrice ?? 0), Number(highestBid.value?.amount ?? 0)))
const isLeadingBidder = computed(() => highestBid.value?.userId === authStore.user?.id)
const canBid = computed(() => authStore.isAuthenticated && product.value?.status === 'ACTIVE' && !isLeadingBidder.value)

onMounted(async () => {
  const id = Number(route.params.id)
  await productStore.fetchById(id)
  await bidStore.fetchByProduct(id)
  if (product.value) {
    bidAmount.value = effectiveCurrentPrice.value + 1
  }
})

async function placeBid() {
  if (!product.value || !authStore.user) return
  bidError.value = ''

  if (isLeadingBidder.value) {
    bidError.value = 'No puedes volver a pujar mientras sigues en cabeza'
    return
  }

  if (bidAmount.value <= effectiveCurrentPrice.value) {
    bidError.value = 'La puja debe ser mayor al precio actual'
    return
  }

  try {
    await bidStore.create(product.value.id, authStore.user.id, Number(bidAmount.value))
    await productStore.fetchById(product.value.id)
    await bidStore.fetchByProduct(product.value.id)
    bidAmount.value = effectiveCurrentPrice.value + 1
    ElMessage.success('Puja realizada')
  } catch (error: any) {
    bidError.value = error.message
  }
}
</script>
