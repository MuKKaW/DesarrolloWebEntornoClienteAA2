<template>
  <section class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold">Gestion de usuarios</h1>
      <el-button type="primary" @click="openCreate">Nuevo usuario</el-button>
    </div>

    <UserTable :users="userStore.items" @edit="openEdit" @delete="confirmDelete" />

    <div class="flex justify-center">
      <el-pagination
        layout="prev, pager, next"
        :total="userStore.total"
        :page-size="pagination.pageSize"
        :current-page="pagination.page"
        @current-change="changePage"
      />
    </div>

    <el-dialog v-model="dialogOpen" width="600px" :title="editing ? 'Editar usuario' : 'Crear usuario'">
      <UserForm
        :model-value="editing"
        :is-loading="userStore.loading"
        @submit="submitForm"
        @cancel="dialogOpen = false"
      />
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import UserForm from '@/components/forms/UserForm.vue'
import UserTable from '@/components/UserTable.vue'
import { useUserStore } from '@/stores/users'
import type { User } from '@/types'

const userStore = useUserStore()
const dialogOpen = ref(false)
const editing = ref<User | null>(null)
const pagination = reactive({ page: 1, pageSize: 10 })

onMounted(load)

function load() {
  userStore.fetchAll(pagination.page, pagination.pageSize)
}

function changePage(page: number) {
  pagination.page = page
  load()
}

function openCreate() {
  editing.value = null
  dialogOpen.value = true
}

function openEdit(user: User) {
  editing.value = user
  dialogOpen.value = true
}

async function submitForm(values: { email: string; password?: string; role: string }) {
  try {
    if (editing.value) {
      await userStore.update(editing.value.id, { ...values, role: values.role as any })
      ElMessage.success('Usuario actualizado')
    } else {
      await userStore.create(values.email, values.password ?? '', values.role)
      ElMessage.success('Usuario creado')
    }
    dialogOpen.value = false
    load()
  } catch (error: any) {
    ElMessage.error(error.message)
  }
}

async function confirmDelete(user: User) {
  try {
    await ElMessageBox.confirm('Se eliminara el usuario seleccionado', 'Confirmacion', { type: 'warning' })
    await userStore.remove(user.id)
    ElMessage.success('Usuario eliminado')
    load()
  } catch {
  }
}
</script>
