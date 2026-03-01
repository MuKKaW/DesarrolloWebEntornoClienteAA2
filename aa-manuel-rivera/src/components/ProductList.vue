<template>
  <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
    <ProductCard
      v-for="product in products"
      :key="product.id"
      :product="product"
      @select="$emit('select', $event)"
    />
  </div>
</template>

<script setup lang="ts">
import type { Product } from '@/types'
import ProductCard from './ProductCard.vue'

defineProps<{ products: Product[] }>()
defineEmits<{ select: [Product] }>()
</script>