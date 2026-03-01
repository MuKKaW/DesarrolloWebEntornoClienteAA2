<template>
  <el-table :data="users" stripe border>
    <el-table-column prop="email" :label="$t('auth.email')" min-width="220" />
    <el-table-column prop="role" :label="$t('users.role')" width="130" />
    <el-table-column :label="$t('users.created_at')" width="180">
      <template #default="scope">{{ new Date(scope.row.createdAt).toLocaleDateString() }}</template>
    </el-table-column>
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
import type { User } from '@/types'

defineProps<{ users: User[] }>()
defineEmits<{ edit: [User]; delete: [User] }>()
</script>