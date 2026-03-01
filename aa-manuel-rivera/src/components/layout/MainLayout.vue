<template>
  <div class="flex min-h-screen flex-col bg-slate-50 dark:bg-slate-950">
    <Header />
    <main class="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
      <slot />
    </main>
    <Footer />
    <AuthModal />
  </div>
</template>

<script setup lang="ts">
import AuthModal from '@/components/auth/AuthModal.vue'
import Header from './Header.vue'
import Footer from './Footer.vue'
</script>
