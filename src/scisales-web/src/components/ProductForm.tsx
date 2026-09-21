import { useEffect, useState } from 'react'
import type { Product, ProductInput } from '../api/types'

interface Props {
  editing: Product | null
  saving: boolean
  error: string | null
  onSubmit: (input: ProductInput) => void
  onCancel: () => void
}

const empty: ProductInput = { name: '', description: '', price: 0 }

export function ProductForm({ editing, saving, error, onSubmit, onCancel }: Props) {
  const [form, setForm] = useState<ProductInput>(empty)

  // Loading a product into the form is the one case where the parent owns the
  // truth and the field state has to follow it.
  useEffect(() => {
    setForm(
      editing
        ? { name: editing.name, description: editing.description, price: editing.price }
        : empty,
    )
  }, [editing])

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    onSubmit(form)
  }

  return (
    <form className="card form" onSubmit={handleSubmit}>
      <h2>{editing ? `Edit product #${editing.id}` : 'New product'}</h2>

      <label>
        Name
        <input
          value={form.name}
          maxLength={100}
          required
          onChange={event => setForm({ ...form, name: event.target.value })}
        />
      </label>

      <label>
        Description
        <textarea
          value={form.description}
          maxLength={500}
          rows={3}
          onChange={event => setForm({ ...form, description: event.target.value })}
        />
      </label>

      <label>
        Price (USD)
        <input
          type="number"
          min="0.01"
          step="0.01"
          value={form.price}
          required
          onChange={event => setForm({ ...form, price: Number(event.target.value) })}
        />
      </label>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={saving}>
          {saving ? 'Saving...' : editing ? 'Save changes' : 'Create'}
        </button>
        {editing && (
          <button type="button" className="secondary" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
