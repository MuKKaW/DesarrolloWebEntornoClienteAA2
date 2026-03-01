export type UserRole = 'USER' | 'MANAGER' | 'ADMIN'
export type ProductStatus = 'DRAFT' | 'ACTIVE' | 'ENDED'

export interface User {
  id: number
  email: string
  nickname: string
  role: UserRole
  isAdmin: boolean
  createdAt: string
}

export interface Product {
  id: number
  title: string
  description: string
  startPrice: number
  currentPrice: number
  startsAt: string
  endsAt: string
  status: ProductStatus
  createdBy: number
  createdByNickname?: string | null
  lastBidNickname?: string | null
  createdAt?: string
}

export interface Bid {
  id: number
  productId: number
  userId: number
  userNickname?: string | null
  amount: number
  createdAt: string
}

export interface AuthResponse {
  token: string
  user: User
}

export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface DashboardStats {
  totalProducts: number
  totalBids: number
  avgPrice: number
  activeProducts: number
}
