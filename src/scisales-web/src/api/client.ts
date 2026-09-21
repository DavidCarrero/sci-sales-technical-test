import type {
  PagedResponse,
  ProblemDetails,
  Product,
  ProductInput,
  ProductPrice,
} from './types'

/**
 * A failed request. It keeps the API's stable `code` so the UI can react to a
 * specific failure instead of matching on message text.
 */
export class ApiError extends Error {
  status: number
  code?: string

  constructor(message: string, status: number, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })

  if (response.status === 204) {
    return undefined as T
  }

  if (!response.ok) {
    const problem = await readProblem(response)

    throw new ApiError(
      problem?.detail ?? problem?.title ?? `Request failed with ${response.status}.`,
      response.status,
      problem?.code,
    )
  }

  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    // A gateway error or a dropped connection has no JSON body.
    return null
  }
}

export const api = {
  list: (page: number, pageSize: number) =>
    request<PagedResponse<Product>>(`/api/products?page=${page}&pageSize=${pageSize}`),

  create: (input: ProductInput) =>
    request<Product>('/api/products', { method: 'POST', body: JSON.stringify(input) }),

  update: (id: number, input: ProductInput) =>
    request<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(input) }),

  remove: (id: number) => request<void>(`/api/products/${id}`, { method: 'DELETE' }),

  price: (id: number, currency: string) =>
    request<ProductPrice>(`/api/products/${id}/price?currency=${encodeURIComponent(currency)}`),
}
