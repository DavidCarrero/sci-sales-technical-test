import { useEffect, useState } from 'react'
import { ApiError, api } from '../api/client'
import type { Product, ProductPrice } from '../api/types'

interface Props {
  product: Product | null
  currency: string
  onCurrencyChange: (currency: string) => void
}

const currencies = ['COP', 'EUR', 'MXN', 'BRL', 'GBP', 'JPY', 'CAD', 'USD']

/**
 * The API consumption seen from the outside: the catalog prices in USD and this
 * panel shows what a product costs today in another currency.
 */
export function PricePanel({ product, currency, onCurrencyChange }: Props) {
  const [price, setPrice] = useState<ProductPrice | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!product) {
      setPrice(null)
      return
    }

    // The panel can be left before the answer arrives, or the currency changed
    // again; this flag keeps a stale answer from overwriting a newer one.
    let current = true

    setLoading(true)
    setError(null)

    api
      .price(product.id, currency)
      .then(result => {
        if (current) setPrice(result)
      })
      .catch((failure: unknown) => {
        if (!current) return

        setPrice(null)
        setError(
          failure instanceof ApiError && failure.status === 503
            ? 'The exchange rate provider did not answer. Try again in a moment.'
            : failure instanceof Error
              ? failure.message
              : 'The price could not be converted.',
        )
      })
      .finally(() => {
        if (current) setLoading(false)
      })

    return () => {
      current = false
    }
  }, [product, currency])

  if (!product) {
    return (
      <section className="card price">
        <h2>Price in another currency</h2>
        <p className="muted">Pick a product from the table to convert its price.</p>
      </section>
    )
  }

  return (
    <section className="card price">
      <h2>Price in another currency</h2>

      <p className="chosen">
        <strong>{product.name}</strong> costs {product.price.toFixed(2)} USD in the catalog.
      </p>

      <label>
        Convert to
        <select value={currency} onChange={event => onCurrencyChange(event.target.value)}>
          {currencies.map(code => (
            <option key={code} value={code}>
              {code}
            </option>
          ))}
        </select>
      </label>

      {loading && <p className="muted">Asking the rate provider...</p>}
      {error && <p className="error">{error}</p>}

      {price && !loading && !error && (
        <div className="result">
          <p className="amount">
            {new Intl.NumberFormat('en-US', {
              style: 'currency',
              currency: price.targetCurrency,
              maximumFractionDigits: 2,
            }).format(price.convertedPrice)}
          </p>
          <p className="muted">
            Rate {price.rate} {price.baseCurrency} to {price.targetCurrency}, published{' '}
            {new Date(price.rateAsOf).toLocaleString()}
          </p>
        </div>
      )}
    </section>
  )
}
