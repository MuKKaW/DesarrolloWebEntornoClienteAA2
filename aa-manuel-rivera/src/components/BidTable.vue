<template>
  <el-table :data="bids" stripe border>
    <el-table-column prop="amount" label="Cantidad" width="150">
      <template #default="scope">{{ scope.row.amount.toFixed(2) }} EUR</template>
    </el-table-column>
    <el-table-column label="Usuario" min-width="160">
      <template #default="scope">{{ scope.row.userNickname || `#${scope.row.userId}` }}</template>
    </el-table-column>
    <el-table-column prop="createdAt" label="Fecha">
      <template #default="scope">{{ new Date(scope.row.createdAt).toLocaleString() }}</template>
    </el-table-column>
  </el-table>
</template>

<script setup lang="ts">
import type { Bid } from '@/types'

defineProps<{ bids: Bid[] }>()
</script>
