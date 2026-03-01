<template>
  <div class="flex min-h-screen flex-col bg-slate-100 dark:bg-slate-950">
    <AdminHeader />
    <div class="flex flex-1">
      <aside class="w-72 border-r border-slate-300 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        <nav class="flex flex-col gap-2">
          <router-link v-if="authStore.isAdmin" to="/admin/auctions" :class="navClass('/admin/auctions')">Subastas</router-link>
          <router-link to="/admin" :class="navClass('/admin')">Dashboard</router-link>
          <router-link to="/admin/products" :class="navClass('/admin/products')">Productos</router-link>
          <router-link v-if="authStore.isAdmin" to="/admin/users" :class="navClass('/admin/users')">Usuarios</router-link>
        </nav>
      </aside>

      <main class="flex-1 p-4 sm:p-6">
        <slot />
      </main>
    </div>
    <AdminFooter />
  </div>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AdminHeader from './AdminHeader.vue'
import AdminFooter from './AdminFooter.vue'

const route = useRoute()
const authStore = useAuthStore()

const navClass = (path: string) => {
  const active = route.path === path
  return [
    'rounded-md px-3 py-2 text-sm font-medium transition',
    active
      ? 'bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900'
      : 'text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800',
  ]
}
</script>
