<template>
  <Form class="space-y-4" :initial-values="initialValues" @submit="onSubmit">
    <div>
      <label class="mb-1 block text-sm">{{ $t('products.title') }}</label>
      <Field name="title" rules="required|min:3" v-slot="{ field, errorMessage }">
        <input v-bind="field" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
        <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
      </Field>
    </div>

    <div>
      <label class="mb-1 block text-sm">{{ $t('products.description') }}</label>
      <Field name="description" rules="required|min:8" v-slot="{ field, errorMessage }">
        <textarea v-bind="field" rows="3" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
        <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
      </Field>
    </div>

    <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
      <div>
        <label class="mb-1 block text-sm">{{ $t('products.start_price') }}</label>
        <Field name="startPrice" rules="required|min_value:0.01" v-slot="{ field, errorMessage }">
          <input v-bind="field" type="number" step="0.01" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
          <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
        </Field>
      </div>

      <div>
        <label class="mb-1 block text-sm">{{ $t('products.current_price') }}</label>
        <Field name="currentPrice" rules="required|min_value:0.01" v-slot="{ field, errorMessage }">
          <input v-bind="field" type="number" step="0.01" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
          <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
        </Field>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
      <div>
        <label class="mb-1 block text-sm">{{ $t('products.starts_at') }}</label>
        <Field name="startsAt" rules="required" v-slot="{ field, errorMessage }">
          <input v-bind="field" type="datetime-local" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
          <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
        </Field>
      </div>

      <div>
        <label class="mb-1 block text-sm">{{ $t('products.ends_at') }}</label>
        <Field name="endsAt" rules="required" v-slot="{ field, errorMessage }">
          <input v-bind="field" type="datetime-local" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
          <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
        </Field>
      </div>
    </div>

    <div>
      <label class="mb-1 block text-sm">{{ $t('products.status') }}</label>
      <Field name="status" rules="required" v-slot="{ field, errorMessage }">
        <select v-bind="field" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900">
          <option value="DRAFT">{{ $t('products.draft') }}</option>
          <option value="ACTIVE">{{ $t('products.active') }}</option>
          <option value="ENDED">{{ $t('products.ended') }}</option>
        </select>
        <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
      </Field>
    </div>

    <div class="flex gap-2">
      <el-button native-type="submit" type="primary" :loading="isLoading">{{ $t('common.save') }}</el-button>
      <el-button @click="$emit('cancel')">{{ $t('common.cancel') }}</el-button>
    </div>
  </Form>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Form, Field } from 'vee-validate'
import type { Product } from '@/types'

const props = defineProps<{ isLoading?: boolean; modelValue?: Product | null }>()
const emit = defineEmits<{ submit: [any]; cancel: [] }>()

const initialValues = computed(() => {
  if (props.modelValue) {
    return {
      ...props.modelValue,
      startsAt: props.modelValue.startsAt?.slice(0, 16),
      endsAt: props.modelValue.endsAt?.slice(0, 16),
    }
  }
  return { title: '', description: '', startPrice: 1, currentPrice: 1, startsAt: '', endsAt: '', status: 'DRAFT' }
})

const onSubmit = (values: any) => {
  emit('submit', {
    ...values,
    startPrice: Number(values.startPrice),
    currentPrice: Number(values.currentPrice),
  })
}
</script>