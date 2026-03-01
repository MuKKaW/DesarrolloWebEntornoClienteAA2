import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import api from '@/services/api'
import type { AuthResponse, User, UserRole } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => Boolean(token.value))
  const userRole = computed<UserRole | null>(() => user.value?.role ?? null)
  const isAdmin = computed(() => Boolean(user.value?.isAdmin || user.value?.role === 'ADMIN'))

  const initializeAuth = () => {
    const savedToken = localStorage.getItem('auth_token')
    const savedUser = localStorage.getItem('auth_user')
    if (savedToken && savedUser) {
      token.value = savedToken
      user.value = JSON.parse(savedUser)
    }
  }

  const login = async (email: string, password: string) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<AuthResponse>('/auth/login', { email: email.trim().toLowerCase(), password })
      token.value = response.data.token
      user.value = response.data.user
      localStorage.setItem('auth_token', token.value)
      localStorage.setItem('auth_user', JSON.stringify(user.value))
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Login failed'
      throw new Error(error.value ?? 'Auth error')
    } finally {
      loading.value = false
    }
  }

  const register = async (email: string, password: string, nickname: string) => {
    loading.value = true
    error.value = null
    try {
      const response = await api.post<AuthResponse>('/auth/register', {
        email: email.trim().toLowerCase(),
        password,
        nickname: nickname.trim(),
      })
      token.value = response.data.token
      user.value = response.data.user
      localStorage.setItem('auth_token', token.value)
      localStorage.setItem('auth_user', JSON.stringify(user.value))
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message ?? 'Register failed'
      throw new Error(error.value ?? 'Auth error')
    } finally {
      loading.value = false
    }
  }

  const logout = () => {
    token.value = null
    user.value = null
    localStorage.removeItem('auth_token')
    localStorage.removeItem('auth_user')
  }

  return {
    user,
    token,
    loading,
    error,
    isAuthenticated,
    userRole,
    isAdmin,
    initializeAuth,
    login,
    register,
    logout,
  }
})
