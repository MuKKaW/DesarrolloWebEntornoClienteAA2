<template>
  <header class="sticky top-0 z-50 border-b border-slate-200 bg-white/95 backdrop-blur dark:border-slate-800 dark:bg-slate-950/95">
    <div class="mx-auto flex h-16 w-full max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
      <router-link to="/" class="text-lg font-bold text-slate-900 dark:text-slate-100">
        Subastas Live
      </router-link>

      <nav class="hidden items-center gap-2 md:flex">
        <router-link to="/" :class="linkClass('home')">{{ $t('common.home') }}</router-link>
        <router-link to="/catalog" :class="linkClass('catalog')">{{ $t('common.catalog') }}</router-link>
        <router-link
          v-if="authStore.isAuthenticated && authStore.isAdmin"
          to="/admin/auctions"
          :class="linkClass('admin')"
        >
          {{ $t('common.admin') }}
        </router-link>
      </nav>

      <div class="flex items-center gap-2">
        <el-select
          :model-value="uiStore.language"
          size="small"
          style="width: 84px"
          @change="changeLanguage"
        >
          <el-option label="ES" value="es" />
          <el-option label="EN" value="en" />
        </el-select>

        <el-button size="small" @click="uiStore.toggleTheme()">
          {{ uiStore.theme === 'dark' ? $t('common.light') : $t('common.dark') }}
        </el-button>

        <template v-if="!authStore.isAuthenticated">
          <el-button type="primary" size="small" @click="router.push('/login')">{{ $t('common.login') }}</el-button>
          <el-button size="small" @click="router.push('/register')">{{ $t('common.register') }}</el-button>
        </template>

        <template v-else>
          <span class="hidden text-sm text-slate-600 dark:text-slate-300 sm:inline">{{ authStore.user?.nickname || authStore.user?.email }}</span>
          <el-button type="danger" size="small" @click="logout">{{ $t('common.logout') }}</el-button>
        </template>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useUiStore } from '@/stores/ui'

const route = useRoute()
const router = useRouter()
const { locale } = useI18n()
const authStore = useAuthStore()
const uiStore = useUiStore()

const linkClass = (section: string) => {
  const active = route.meta.section === section
  return [
    'rounded-md px-3 py-2 text-sm font-semibold transition',
    active
      ? 'bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900'
      : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
  ]
}

const changeLanguage = (value: 'es' | 'en') => {
  uiStore.setLanguage(value)
  locale.value = value
}

const logout = () => {
  authStore.logout()
  router.push('/')
}
</script>
