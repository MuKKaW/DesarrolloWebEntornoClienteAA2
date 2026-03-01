<template>
  <article class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm transition hover:-translate-y-1 hover:shadow-lg dark:border-slate-700 dark:bg-slate-900">
    <div class="flex h-40 items-center justify-center bg-gradient-to-br from-blue-600 to-cyan-500 text-4xl text-white">AU</div>
    <div class="space-y-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h3 class="line-clamp-2 text-lg font-semibold">{{ product.title }}</h3>
        <el-tag :type="product.status === 'ACTIVE' ? 'success' : product.status === 'DRAFT' ? 'warning' : 'info'" size="small">
          {{ product.status }}
        </el-tag>
      </div>

      <p class="line-clamp-2 text-sm text-slate-600 dark:text-slate-300">{{ product.description }}</p>

      <div class="flex items-center justify-between text-sm">
        <span class="text-slate-500">{{ $t('products.current_price') }}</span>
        <strong>{{ product.currentPrice.toFixed(2) }} EUR</strong>
      </div>

      <el-button type="primary" plain class="w-full" @click="$emit('select', product)">
        {{ $t('products.view_details') }}
      </el-button>
    </div>
  </article>
</template>

<script setup lang="ts">
import type { Product } from '@/types'

defineProps<{ product: Product }>()
defineEmits<{ select: [Product] }>()
</script>