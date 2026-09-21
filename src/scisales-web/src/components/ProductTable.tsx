import type { Product } from '../api/types'

interface Props {
  products: Product[]
  loading: boolean
  selectedId: number | null
  onEdit: (product: Product) => void
  onDelete: (product: Product) => void
  onSelect: (product: Product) => void
}

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const date = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium' })

export function ProductTable({
  products,
  loading,
  selectedId,
  onEdit,
  onDelete,
  onSelect,
}: Props) {
  if (!loading && products.length === 0) {
    return <p className="empty">No products yet. Create the first one with the form.</p>
  }

  return (
    <table className={loading ? 'loading' : undefined}>
      <thead>
        <tr>
          <th className="numeric">Id</th>
          <th>Name</th>
          <th>Description</th>
          <th className="numeric">Price</th>
          <th>Created</th>
          <th aria-label="Actions" />
        </tr>
      </thead>
      <tbody>
        {products.map(product => (
          <tr
            key={product.id}
            className={product.id === selectedId ? 'selected' : undefined}
            onClick={() => onSelect(product)}
          >
            <td className="numeric">{product.id}</td>
            <td>{product.name}</td>
            <td className="muted">{product.description}</td>
            <td className="numeric">{money.format(product.price)}</td>
            <td className="muted">{date.format(new Date(product.createdDate))}</td>
            <td className="row-actions">
              <button
                type="button"
                className="link"
                onClick={event => {
                  event.stopPropagation()
                  onEdit(product)
                }}
              >
                Edit
              </button>
              <button
                type="button"
                className="link danger"
                onClick={event => {
                  event.stopPropagation()
                  onDelete(product)
                }}
              >
                Delete
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
