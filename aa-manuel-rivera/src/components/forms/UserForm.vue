<template>
  <Form class="space-y-4" :initial-values="initialValues" @submit="onSubmit">
    <div>
      <label class="mb-1 block text-sm">Nick / Apodo</label>
      <Field name="nickname" rules="required|min:3|max:30|alpha_dash" v-slot="{ field, errorMessage }">
        <input v-bind="field" type="text" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
        <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
      </Field>
    </div>

    <div>
      <label class="mb-1 block text-sm">{{ $t('auth.email') }}</label>
      <Field name="email" rules="required|email" v-slot="{ field, errorMessage }">
        <input v-bind="field" type="email" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
        <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
      </Field>
    </div>

    <div>
      <label class="mb-1 block text-sm">{{ $t('auth.password') }}</label>
      <Field :rules="passwordRules" name="password" v-slot="{ field, errorMessage }">
        <input v-bind="field" type="password" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900" />
        <p class="mt-1 text-xs text-red-500">{{ errorMessage }}</p>
      </Field>
    </div>

    <div>
      <label class="mb-1 block text-sm">{{ $t('users.role') }}</label>
      <Field name="role" rules="required" v-slot="{ field, errorMessage }">
        <select v-bind="field" class="w-full rounded-md border border-slate-300 px-3 py-2 dark:border-slate-700 dark:bg-slate-900">
          <option value="USER">{{ $t('users.user') }}</option>
          <option value="MANAGER">{{ $t('users.manager') }}</option>
          <option value="ADMIN">{{ $t('users.admin') }}</option>
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
import type { User } from '@/types'

const props = defineProps<{ isLoading?: boolean; modelValue?: User | null }>()
const emit = defineEmits<{ submit: [any]; cancel: [] }>()

const initialValues = computed(() => ({
  nickname: props.modelValue?.nickname ?? '',
  email: props.modelValue?.email ?? '',
  password: '',
  role: props.modelValue?.role ?? 'USER',
}))

const passwordRules = computed(() => (props.modelValue ? '' : 'required|min:6'))

const onSubmit = (values: any) => {
  emit('submit', values)
}
</script>
