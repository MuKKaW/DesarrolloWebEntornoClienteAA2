<template>
  <div
    :class="[
      'p-4 rounded-lg border-l-4 shadow-sm',
      typeClasses,
    ]"
  >
    <div class="flex items-start">
      <div class="flex-shrink-0 text-lg">{{ icon }}</div>
      <div class="ml-3">
        <slot />
      </div>
      <button
        v-if="closeable"
        @click="visible = false"
        class="ml-auto text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
      >
        ✕
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

interface Props {
  type?: 'success' | 'error' | 'warning' | 'info'
  closeable?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  type: 'info',
  closeable: true,
})

const visible = ref(true)

const typeClasses = computed(() => {
  const types = {
    success: 'bg-green-50 border-green-400 text-green-800 dark:bg-green-900 dark:border-green-700 dark:text-green-200',
    error: 'bg-red-50 border-red-400 text-red-800 dark:bg-red-900 dark:border-red-700 dark:text-red-200',
    warning: 'bg-yellow-50 border-yellow-400 text-yellow-800 dark:bg-yellow-900 dark:border-yellow-700 dark:text-yellow-200',
    info: 'bg-blue-50 border-blue-400 text-blue-800 dark:bg-blue-900 dark:border-blue-700 dark:text-blue-200',
  }
  return types[props.type]
})

const icon = computed(() => {
  const icons = {
    success: '✓',
    error: '✕',
    warning: '⚠',
    info: 'ℹ',
  }
  return icons[props.type]
})
</script>
