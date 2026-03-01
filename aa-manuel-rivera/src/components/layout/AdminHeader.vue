<template>
  <header class="border-b border-slate-300 bg-slate-900 text-slate-100">
    <div class="mx-auto flex h-16 w-full max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
      <div class="flex items-center gap-3">
        <el-button size="small" @click="uiStore.toggleSidebar()">Menu</el-button>
        <el-button size="small" @click="router.push('/')">Inicio</el-button>
        <h1 class="text-base font-semibold">Panel de Administracion</h1>
      </div>

      <div class="flex items-center gap-2">
        <span class="text-sm text-slate-300">{{ authStore.user?.role }}</span>
        <el-button size="small" type="danger" @click="logout">Salir</el-button>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'

const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()

const logout = () => {
  authStore.logout()
  router.push('/')
}
</script>
