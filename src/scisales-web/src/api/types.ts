export interface Product {
  id: number
  name: string
  description: string
  price: number
  currency: string
  createdDate: string
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ProductPrice {
  productId: number
  name: string
  basePrice: number
  baseCurrency: string
  convertedPrice: number
  targetCurrency: string
  rate: number
  rateAsOf: string
}

export interface ProductInput {
  name: string
  description: string
  price: number
}

/** RFC 9457 problem details, plus the stable `code` the API adds. */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
}
