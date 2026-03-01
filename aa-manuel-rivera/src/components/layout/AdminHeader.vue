<template>
  <header class="border-b border-slate-300 bg-slate-900 text-slate-100">
    <div class="mx-auto flex h-16 w-full max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
      <div class="flex items-center gap-3">
        <el-button size="small" @click="goHome">Inicio</el-button>
        <h1 class="text-base font-semibold">Panel de Administracion</h1>
      </div>

      <div class="flex items-center gap-2">
        <span class="text-sm font-semibold text-slate-200">{{ authStore.user?.nickname || authStore.user?.email }}</span>
        <span class="text-sm text-slate-300">{{ authStore.user?.role }}</span>
        <el-button size="small" type="danger" @click="logout">Salir</el-button>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const goHome = () => {
  router.push('/')
}

const logout = () => {
  authStore.logout()
  router.push('/')
}
</script>
