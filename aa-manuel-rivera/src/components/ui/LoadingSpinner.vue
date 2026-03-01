<template>
  <div class="flex items-center justify-center">
    <div
      :class="[
        'rounded-full border-4 border-gray-200 dark:border-gray-700 animate-spin',
        sizeClasses,
        'border-t-blue-600'
      ]"
    ></div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  size?: 'sm' | 'md' | 'lg'
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
})

const sizeClasses = computed(() => {
  const sizes = {
    sm: 'w-6 h-6',
    md: 'w-10 h-10',
    lg: 'w-16 h-16',
  }
  return sizes[props.size]
})
</script>
