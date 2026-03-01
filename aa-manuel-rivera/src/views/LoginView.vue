<template>
  <section class="mx-auto w-full max-w-md space-y-6 text-slate-100">
    <div class="space-y-2 text-center">
      <h1 class="text-3xl font-bold">{{ $t('auth.login_title') }}</h1>
      <p class="text-sm text-slate-300">Accede para pujar y gestionar tus subastas.</p>
    </div>

    <div class="rounded-xl border border-slate-700 bg-slate-900/80 p-6 shadow-2xl">
      <LoginForm :is-loading="authStore.loading" @submit="handleLogin" />

      <p v-if="errorMessage" class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-600">
        {{ errorMessage }}
      </p>

      <p class="mt-5 text-center text-sm text-slate-400">
        {{ $t('auth.no_account') }}
        <router-link class="font-semibold text-cyan-400" to="/register">{{ $t('common.register') }}</router-link>
      </p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import LoginForm from '@/components/forms/LoginForm.vue'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const errorMessage = ref('')

if (authStore.isAuthenticated) {
  router.replace(authStore.isAdmin ? '/admin/auctions' : '/catalog')
}

async function handleLogin(payload: { email: string; password: string }) {
  try {
    errorMessage.value = ''
    const response = await authStore.login(payload.email, payload.password)
    router.push(response.user.isAdmin ? '/admin/auctions' : '/catalog')
  } catch (error: any) {
    errorMessage.value = error.message
  }
}
</script>
