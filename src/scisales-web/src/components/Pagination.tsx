interface Props {
  page: number
  pageSize: number
  totalPages: number
  totalItems: number
  hasPrevious: boolean
  hasNext: boolean
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

const sizes = [5, 10, 20, 50]

/** At most seven buttons: the current page with three neighbours on each side. */
function window(page: number, totalPages: number): number[] {
  const start = Math.max(1, Math.min(page - 3, totalPages - 6))
  const end = Math.min(totalPages, start + 6)

  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
}

export function Pagination({
  page,
  pageSize,
  totalPages,
  totalItems,
  hasPrevious,
  hasNext,
  onPageChange,
  onPageSizeChange,
}: Props) {
  const from = totalItems === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalItems)

  return (
    <nav className="pagination" aria-label="Pages of products">
      <span className="range muted">
        {from} to {to} of {totalItems}
      </span>

      <div className="pages">
        <button
          type="button"
          className="page-button"
          disabled={!hasPrevious}
          aria-label="Previous page"
          onClick={() => onPageChange(page - 1)}
        >
          Previous
        </button>

        {window(page, Math.max(totalPages, 1)).map(number => (
          <button
            key={number}
            type="button"
            className={number === page ? 'page-button current' : 'page-button'}
            aria-current={number === page ? 'page' : undefined}
            onClick={() => onPageChange(number)}
          >
            {number}
          </button>
        ))}

        <button
          type="button"
          className="page-button"
          disabled={!hasNext}
          aria-label="Next page"
          onClick={() => onPageChange(page + 1)}
        >
          Next
        </button>
      </div>

      <label className="page-size">
        Rows
        <select
          value={pageSize}
          onChange={event => onPageSizeChange(Number(event.target.value))}
        >
          {sizes.map(size => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>
    </nav>
  )
}
