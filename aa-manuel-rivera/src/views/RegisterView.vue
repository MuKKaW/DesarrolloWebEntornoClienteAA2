<template>
  <section class="mx-auto w-full max-w-md space-y-6 text-slate-100">
    <div class="space-y-2 text-center">
      <h1 class="text-3xl font-bold">{{ $t('auth.register_title') }}</h1>
      <p class="text-sm text-slate-300">Crea cuenta para participar en subastas.</p>
    </div>

    <div class="rounded-xl border border-slate-700 bg-slate-900/80 p-6 shadow-2xl">
      <RegisterForm :is-loading="authStore.loading" @submit="handleRegister" />

      <p v-if="errorMessage" class="mt-4 rounded-md bg-red-50 p-3 text-sm text-red-600">
        {{ errorMessage }}
      </p>

      <p class="mt-5 text-center text-sm text-slate-400">
        {{ $t('auth.have_account') }}
        <router-link class="font-semibold text-cyan-400" to="/login">{{ $t('common.login') }}</router-link>
      </p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import RegisterForm from '@/components/forms/RegisterForm.vue'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const errorMessage = ref('')

if (authStore.isAuthenticated) {
  router.replace(authStore.isAdmin ? '/admin/auctions' : '/catalog')
}

async function handleRegister(payload: { email: string; password: string; nickname: string }) {
  try {
    errorMessage.value = ''
    await authStore.register(payload.email, payload.password, payload.nickname)
    router.push('/catalog')
  } catch (error: any) {
    errorMessage.value = error.message
  }
}
</script>
