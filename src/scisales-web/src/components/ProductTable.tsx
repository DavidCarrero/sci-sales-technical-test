import { useState } from 'react'
import type { Product } from '../api/types'

interface Props {
  products: Product[]
  loading: boolean
  selectedId: number | null
  onEdit: (product: Product) => void
  onDelete: (product: Product) => void
  onSelect: (product: Product) => void
}

const amount = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})
const shortDate = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium' })
const fullDate = new Intl.DateTimeFormat('en-GB', { dateStyle: 'full', timeStyle: 'short' })

/** "3 days ago" reads faster than a date when the row was just created. */
function relative(value: Date): string {
  const days = Math.round((Date.now() - value.getTime()) / 86_400_000)

  if (days <= 0) return 'today'
  if (days === 1) return 'yesterday'
  if (days < 30) return plural(days, 'day')
  if (days < 365) return plural(Math.round(days / 30), 'month')

  return plural(Math.round(days / 365), 'year')
}

function plural(count: number, unit: string): string {
  return `${count} ${unit}${count === 1 ? '' : 's'} ago`
}

export function ProductTable({
  products,
  loading,
  selectedId,
  onEdit,
  onDelete,
  onSelect,
}: Props) {
  // Deleting asks twice, in the row itself. A window.confirm would block the
  // page, and any tool driving the browser dismisses it, which makes a correct
  // delete look broken.
  const [confirmingId, setConfirmingId] = useState<number | null>(null)

  if (loading && products.length === 0) {
    return <p className="empty">Loading the catalog...</p>
  }

  if (products.length === 0) {
    return <p className="empty">No products yet. Create the first one with the form.</p>
  }

  return (
    <div className="table-wrap">
      <table className={loading ? 'loading' : undefined}>
        <thead>
          <tr>
            <th className="col-id numeric">Id</th>
            <th className="col-name">Product</th>
            <th className="col-price numeric">Price</th>
            <th className="col-date">Created</th>
            <th className="col-actions">Actions</th>
          </tr>
        </thead>
        <tbody>
          {products.map(product => {
            const created = new Date(product.createdDate)
            const confirming = confirmingId === product.id

            return (
              <tr
                key={product.id}
                className={product.id === selectedId ? 'selected' : undefined}
                onClick={() => onSelect(product)}
              >
                <td className="col-id numeric">
                  <span className="id">#{product.id}</span>
                </td>

                <td className="col-name">
                  <span className="name">{product.name}</span>
                  {product.description && (
                    <span className="description" title={product.description}>
                      {product.description}
                    </span>
                  )}
                </td>

                <td className="col-price numeric">
                  <span className="price-amount">{amount.format(product.price)}</span>
                  <span className="currency">{product.currency}</span>
                </td>

                <td className="col-date">
                  <span title={fullDate.format(created)}>{shortDate.format(created)}</span>
                  <span className="ago">{relative(created)}</span>
                </td>

                <td className="col-actions">
                  {confirming ? (
                    <>
                      <button
                        type="button"
                        className="chip danger solid"
                        onClick={event => {
                          event.stopPropagation()
                          setConfirmingId(null)
                          onDelete(product)
                        }}
                      >
                        Confirm
                      </button>
                      <button
                        type="button"
                        className="chip"
                        onClick={event => {
                          event.stopPropagation()
                          setConfirmingId(null)
                        }}
                      >
                        Cancel
                      </button>
                    </>
                  ) : (
                    <>
                      <button
                        type="button"
                        className="chip"
                        onClick={event => {
                          event.stopPropagation()
                          onEdit(product)
                        }}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="chip danger"
                        onClick={event => {
                          event.stopPropagation()
                          setConfirmingId(product.id)
                        }}
                      >
                        Delete
                      </button>
                    </>
                  )}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
