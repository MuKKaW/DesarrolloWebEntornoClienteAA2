<template>
  <el-dialog
    :model-value="isOpen"
    width="480px"
    :show-close="true"
    :destroy-on-close="false"
    @close="uiStore.closeAuthModal()"
  >
    <template #header>
      <div class="space-y-1">
        <h2 class="text-xl font-semibold">
          {{ mode === 'login' ? $t('auth.login_title') : $t('auth.register_title') }}
        </h2>
        <p class="text-sm text-slate-500">
          {{ mode === 'login' ? 'Accede para pujar y gestionar tus subastas.' : 'Crea cuenta para participar en subastas.' }}
        </p>
      </div>
    </template>

    <div class="space-y-5">
      <LoginForm v-if="mode === 'login'" :is-loading="authStore.loading" @submit="handleLogin" />
      <RegisterForm v-else :is-loading="authStore.loading" @submit="handleRegister" />

      <p v-if="errorMessage" class="rounded-md bg-red-50 p-3 text-sm text-red-600">
        {{ errorMessage }}
      </p>

      <p class="text-center text-sm text-slate-500">
        <template v-if="mode === 'login'">
          {{ $t('auth.no_account') }}
          <button class="font-semibold text-cyan-600" type="button" @click="uiStore.openAuthModal('register')">
            {{ $t('common.register') }}
          </button>
        </template>
        <template v-else>
          {{ $t('auth.have_account') }}
          <button class="font-semibold text-cyan-600" type="button" @click="uiStore.openAuthModal('login')">
            {{ $t('common.login') }}
          </button>
        </template>
      </p>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import LoginForm from '@/components/forms/LoginForm.vue'
import RegisterForm from '@/components/forms/RegisterForm.vue'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const uiStore = useUiStore()
const errorMessage = ref('')

const mode = computed(() => uiStore.authModalMode ?? 'login')
const isOpen = computed(() => uiStore.authModalMode !== null)

watch(isOpen, () => {
  errorMessage.value = ''
})

async function handleLogin(payload: { email: string; password: string }) {
  try {
    errorMessage.value = ''
    const response = await authStore.login(payload.email, payload.password)
    uiStore.closeAuthModal()

    if (response.user.isAdmin) {
      router.push('/admin/auctions')
      return
    }

    if (route.path === '/login' || route.path === '/register') {
      router.push('/catalog')
    }
  } catch (error: any) {
    errorMessage.value = error.message
  }
}

async function handleRegister(payload: { email: string; password: string }) {
  try {
    errorMessage.value = ''
    await authStore.register(payload.email, payload.password)
    uiStore.closeAuthModal()

    if (route.path === '/login' || route.path === '/register') {
      router.push('/catalog')
    }
  } catch (error: any) {
    errorMessage.value = error.message
  }
}
</script>
