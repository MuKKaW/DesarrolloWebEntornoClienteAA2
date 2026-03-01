<template>
  <div />
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'

const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()

onMounted(() => {
  if (authStore.isAuthenticated) {
    router.replace(authStore.isAdmin ? '/admin/auctions' : '/catalog')
    return
  }

  uiStore.openAuthModal('register')
  router.replace('/')
})
</script>
