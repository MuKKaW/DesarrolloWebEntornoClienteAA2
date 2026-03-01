<template>
  <section class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-2xl font-bold">Dashboard</h1>
      <div class="flex gap-2">
        <el-input v-model="startDate" type="date" />
        <el-input v-model="endDate" type="date" />
        <el-button :loading="dashboardStore.loading" @click="load">Filtrar</el-button>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-4 md:grid-cols-4">
      <el-card><el-statistic title="Total productos" :value="dashboardStore.kpis.totalProducts" /></el-card>
      <el-card><el-statistic title="Total pujas" :value="dashboardStore.kpis.totalBids" /></el-card>
      <el-card><el-statistic title="Precio medio" :value="dashboardStore.kpis.avgPrice" suffix="EUR" /></el-card>
      <el-card><el-statistic title="Productos activos" :value="dashboardStore.kpis.activeProducts" /></el-card>
    </div>

    <div class="grid grid-cols-1 gap-4 xl:grid-cols-3">
      <el-card>
        <template #header>Pujas por fecha</template>
        <div class="h-[320px]"><Line :data="lineData" :options="chartOptions" /></div>
      </el-card>

      <el-card>
        <template #header>Top productos</template>
        <div class="h-[320px]"><Bar :data="barData" :options="chartOptions" /></div>
      </el-card>

      <el-card>
        <template #header>Estado productos</template>
        <div class="h-[320px]"><Doughnut :data="pieData" :options="chartOptions" /></div>
      </el-card>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Bar, Doughnut, Line } from 'vue-chartjs'
import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
  Filler,
} from 'chart.js'
import { useDashboardStore } from '@/stores/dashboard'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, BarElement, ArcElement, Tooltip, Legend, Filler)

const dashboardStore = useDashboardStore()
const startDate = ref('')
const endDate = ref('')

const lineData = computed(() => ({
  labels: dashboardStore.bidsByDate.map((x) => x.date),
  datasets: [
    {
      label: 'Pujas',
      data: dashboardStore.bidsByDate.map((x) => x.count),
      borderColor: '#2563eb',
      backgroundColor: 'rgba(37, 99, 235, 0.2)',
      tension: 0.25,
      fill: true,
    },
  ],
}))

const barData = computed(() => ({
  labels: dashboardStore.topProducts.map((x) => x.title),
  datasets: [
    {
      label: 'Numero de pujas',
      data: dashboardStore.topProducts.map((x) => x.bidCount),
      backgroundColor: ['#0ea5e9', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6'],
    },
  ],
}))

const pieData = computed(() => ({
  labels: ['DRAFT', 'ACTIVE', 'ENDED'],
  datasets: [
    {
      data: [
        dashboardStore.statusDistribution.DRAFT ?? 0,
        dashboardStore.statusDistribution.ACTIVE ?? 0,
        dashboardStore.statusDistribution.ENDED ?? 0,
      ],
      backgroundColor: ['#f59e0b', '#22c55e', '#64748b'],
    },
  ],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
}

onMounted(load)

function load() {
  dashboardStore.load(startDate.value || undefined, endDate.value || undefined)
}
</script>