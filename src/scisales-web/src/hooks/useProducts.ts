import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { PagedResponse, Product } from '../api/types'

/**
 * Loads one page of products and exposes a reload so the page can refresh
 * itself after a create, an edit or a delete.
 */
export function useProducts(page: number, pageSize: number) {
  const [data, setData] = useState<PagedResponse<Product> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)

    try {
      setData(await api.list(page, pageSize))
      setError(null)
    } catch (failure) {
      setError(failure instanceof Error ? failure.message : 'The catalog could not be read.')
    } finally {
      setLoading(false)
    }
  }, [page, pageSize])

  useEffect(() => {
    void load()
  }, [load])

  return { data, loading, error, reload: load }
}
