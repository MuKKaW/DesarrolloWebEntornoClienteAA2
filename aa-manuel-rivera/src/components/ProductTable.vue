<template>
  <el-table :data="products" stripe border>
    <el-table-column prop="title" :label="$t('products.title')" min-width="220" />
    <el-table-column :label="$t('products.start_price')" width="140">
      <template #default="scope">{{ scope.row.startPrice.toFixed(2) }} EUR</template>
    </el-table-column>
    <el-table-column :label="$t('products.current_price')" width="140">
      <template #default="scope">{{ scope.row.currentPrice.toFixed(2) }} EUR</template>
    </el-table-column>
    <el-table-column prop="status" :label="$t('products.status')" width="120" />
    <el-table-column :label="$t('common.actions')" width="220">
      <template #default="scope">
        <div class="flex gap-2">
          <el-button size="small" @click="$emit('edit', scope.row)">{{ $t('common.edit') }}</el-button>
          <el-button size="small" type="danger" @click="$emit('delete', scope.row)">{{ $t('common.delete') }}</el-button>
        </div>
      </template>
    </el-table-column>
  </el-table>
</template>

<script setup lang="ts">
import type { Product } from '@/types'

defineProps<{ products: Product[] }>()
defineEmits<{ edit: [Product]; delete: [Product] }>()
</script>