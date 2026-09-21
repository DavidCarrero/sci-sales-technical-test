import { useState } from 'react'
import { api } from './api/client'
import type { Product, ProductInput } from './api/types'
import { PricePanel } from './components/PricePanel'
import { ProductForm } from './components/ProductForm'
import { ProductTable } from './components/ProductTable'
import { useProducts } from './hooks/useProducts'

const pageSize = 10

export default function App() {
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Product | null>(null)
  const [selected, setSelected] = useState<Product | null>(null)
  const [currency, setCurrency] = useState('COP')
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const products = useProducts(page, pageSize)

  async function handleSubmit(input: ProductInput) {
    setSaving(true)
    setFormError(null)

    try {
      if (editing) {
        await api.update(editing.id, input)
        setEditing(null)
      } else {
        await api.create(input)
      }

      await products.reload()
    } catch (failure) {
      // The API already explains the failure in plain words; showing its own
      // message keeps the rules in one place instead of copying them here.
      setFormError(failure instanceof Error ? failure.message : 'The product could not be saved.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(product: Product) {
    if (!confirm(`Delete "${product.name}"?`)) return

    await api.remove(product.id)

    if (selected?.id === product.id) setSelected(null)
    if (editing?.id === product.id) setEditing(null)

    await products.reload()
  }

  const total = products.data?.totalItems ?? 0
  const totalPages = products.data?.totalPages ?? 1

  return (
    <div className="page">
      <header>
        <h1>SCI Sales catalog</h1>
        <p className="muted">
          Products stored in SQL Server through stored procedures, priced in USD, converted with
          the exchange rate of the day.
        </p>
      </header>

      <main>
        <section className="card list">
          <div className="list-header">
            <h2>Products</h2>
            <span className="muted">{total} in total</span>
          </div>

          {products.error && <p className="error">{products.error}</p>}

          <ProductTable
            products={products.data?.items ?? []}
            loading={products.loading}
            selectedId={selected?.id ?? null}
            onEdit={product => {
              setEditing(product)
              setFormError(null)
            }}
            onDelete={handleDelete}
            onSelect={setSelected}
          />

          <div className="paging">
            <button type="button" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              Previous
            </button>
            <span className="muted">
              Page {page} of {Math.max(totalPages, 1)}
            </span>
            <button
              type="button"
              disabled={!products.data?.hasNextPage}
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        </section>

        <div className="side">
          <ProductForm
            editing={editing}
            saving={saving}
            error={formError}
            onSubmit={handleSubmit}
            onCancel={() => {
              setEditing(null)
              setFormError(null)
            }}
          />

          <PricePanel product={selected} currency={currency} onCurrencyChange={setCurrency} />
        </div>
      </main>

      <footer className="muted">
        <a href="/scalar/v1">API documentation</a>
      </footer>
    </div>
  )
}
