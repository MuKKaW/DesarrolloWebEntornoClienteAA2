import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/views/HomeView.vue'),
    meta: { layout: 'main', section: 'home' },
  },
  {
    path: '/login',
    component: () => import('@/views/LoginView.vue'),
    meta: { layout: 'auth' },
  },
  {
    path: '/register',
    component: () => import('@/views/RegisterView.vue'),
    meta: { layout: 'auth' },
  },
  {
    path: '/catalog',
    component: () => import('@/views/CatalogView.vue'),
    meta: { layout: 'main', section: 'catalog' },
  },
  {
    path: '/products/:id',
    component: () => import('@/views/ProductDetailView.vue'),
    meta: { layout: 'main', section: 'catalog' },
  },
  {
    path: '/admin/auctions',
    component: () => import('@/views/AdminAuctionModerationView.vue'),
    meta: { layout: 'admin', requiresAuth: true, roles: ['ADMIN'], section: 'admin' },
  },
  {
    path: '/admin',
    component: () => import('@/views/AdminDashboardView.vue'),
    meta: { layout: 'admin', requiresAuth: true, roles: ['ADMIN', 'MANAGER'], section: 'dashboard' },
  },
  {
    path: '/admin/products',
    component: () => import('@/views/AdminProductsView.vue'),
    meta: { layout: 'admin', requiresAuth: true, roles: ['ADMIN', 'MANAGER'], section: 'products' },
  },
  {
    path: '/admin/users',
    component: () => import('@/views/AdminUsersView.vue'),
    meta: { layout: 'admin', requiresAuth: true, roles: ['ADMIN'], section: 'users' },
  },
  {
    path: '/:pathMatch(.*)*',
    component: () => import('@/views/NotFoundView.vue'),
    meta: { layout: 'main' },
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    next('/login')
    return
  }

  if (to.meta.roles) {
    const roles = to.meta.roles as string[]
    if (!auth.userRole || !roles.includes(auth.userRole)) {
      next('/')
      return
    }
  }

  next()
})

export default router
