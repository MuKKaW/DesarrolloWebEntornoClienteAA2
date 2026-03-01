<template>
  <component :is="layoutComponent">
    <router-view />
  </component>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'
import MainLayout from '@/components/layout/MainLayout.vue'
import AuthLayout from '@/components/layout/AuthLayout.vue'
import AdminLayout from '@/components/layout/AdminLayout.vue'

const route = useRoute()
const authStore = useAuthStore()
const uiStore = useUiStore()

const layoutComponent = computed(() => {
  const layout = route.meta.layout as string | undefined
  if (layout === 'auth') return AuthLayout
  if (layout === 'admin') return AdminLayout
  return MainLayout
})

onMounted(() => {
  authStore.initializeAuth()
  uiStore.initializeUi()
})
</script>