import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useUiStore = defineStore('ui', () => {
  const theme = ref<'light' | 'dark'>('light')
  const language = ref<'es' | 'en'>('es')
  const sidebarOpen = ref(true)
  const authModalMode = ref<'login' | 'register' | null>(null)

  const applyTheme = () => {
    if (theme.value === 'dark') {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  const initializeUi = () => {
    const savedTheme = localStorage.getItem('theme') as 'light' | 'dark' | null
    const savedLanguage = localStorage.getItem('language') as 'es' | 'en' | null
    if (savedTheme) theme.value = savedTheme
    if (savedLanguage) language.value = savedLanguage
    applyTheme()
  }

  const toggleTheme = () => {
    theme.value = theme.value === 'light' ? 'dark' : 'light'
    localStorage.setItem('theme', theme.value)
    applyTheme()
  }

  const setLanguage = (value: 'es' | 'en') => {
    language.value = value
    localStorage.setItem('language', value)
  }

  const toggleSidebar = () => {
    sidebarOpen.value = !sidebarOpen.value
  }

  const openAuthModal = (mode: 'login' | 'register') => {
    authModalMode.value = mode
  }

  const closeAuthModal = () => {
    authModalMode.value = null
  }

  return {
    theme,
    language,
    sidebarOpen,
    authModalMode,
    initializeUi,
    toggleTheme,
    setLanguage,
    toggleSidebar,
    openAuthModal,
    closeAuthModal,
  }
})
