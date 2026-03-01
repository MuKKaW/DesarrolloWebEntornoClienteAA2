<template>
  <Form class="space-y-4" @submit="onSubmit">
    <div>
      <label class="mb-1 block text-sm text-slate-200">{{ $t('auth.email') }}</label>
      <Field name="email" rules="required|email" v-slot="{ field, errorMessage }">
        <input v-bind="field" type="email" class="w-full rounded-md border border-slate-600 bg-slate-800 px-3 py-2 text-slate-100" />
        <p class="mt-1 text-xs text-red-400">{{ errorMessage }}</p>
      </Field>
    </div>

    <div>
      <label class="mb-1 block text-sm text-slate-200">{{ $t('auth.password') }}</label>
      <Field name="password" rules="required|min:6" v-slot="{ field, errorMessage }">
        <input v-bind="field" type="password" class="w-full rounded-md border border-slate-600 bg-slate-800 px-3 py-2 text-slate-100" />
        <p class="mt-1 text-xs text-red-400">{{ errorMessage }}</p>
      </Field>
    </div>

    <div>
      <label class="mb-1 block text-sm text-slate-200">{{ $t('auth.confirm_password') }}</label>
      <Field name="confirmPassword" rules="required|confirmed:@password" v-slot="{ field, errorMessage }">
        <input v-bind="field" type="password" class="w-full rounded-md border border-slate-600 bg-slate-800 px-3 py-2 text-slate-100" />
        <p class="mt-1 text-xs text-red-400">{{ errorMessage }}</p>
      </Field>
    </div>

    <el-button native-type="submit" type="primary" size="large" class="w-full" :loading="isLoading">
      {{ $t('auth.register_btn') }}
    </el-button>
  </Form>
</template>

<script setup lang="ts">
import { Form, Field } from 'vee-validate'

defineProps<{ isLoading?: boolean }>()

const emit = defineEmits<{ submit: [{ email: string; password: string }] }>()

const onSubmit = (values: any) => {
  emit('submit', { email: values.email, password: values.password })
}
</script>
