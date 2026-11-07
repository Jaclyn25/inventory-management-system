import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { Search, ArrowDownLeft, ArrowUpRight, SlidersHorizontal } from 'lucide-react'
import { api, kindLabel, params, timestamp, type Movement, type Page } from './api'
import { Empty, ErrorMessage, Loading, Pagination } from './ui'

export function MovementTable({ movements }: { movements: Movement[] }) {
  return (
    <div className="table-scroll" tabIndex={0} aria-label="Stock movement history">
      <table>
        <thead>
          <tr>
            <th>Product / Operation</th>
            <th>Movement</th>
            <th className="numeric">Change</th>
            <th className="numeric">Balance</th>
            <th>Reason</th>
            <th>Recorded</th>
          </tr>
        </thead>
        <tbody>
          {movements.map((m) => (
            <tr key={m.id}>
              <td>
                <strong>{m.productName}</strong>
                <span className="subline mono">{m.sku}</span>
                <span className="operation-id" title={m.id}>
                  #{m.id.slice(0, 8)}
                </span>
              </td>
              <td>
                <span className={`movement-kind ${m.kind}`}>
                  {m.kind === 'in' ? (
                    <ArrowDownLeft size={16} />
                  ) : m.kind === 'out' ? (
                    <ArrowUpRight size={16} />
                  ) : (
                    <SlidersHorizontal size={16} />
                  )}{' '}
                  {kindLabel(m.kind)}
                </span>
              </td>
              <td className={`numeric ${m.delta > 0 ? 'positive' : 'negative'}`}>
                <strong>
                  {m.delta > 0 ? '+' : ''}
                  {m.delta}
                </strong>
              </td>
              <td className="numeric">
                <strong>{m.quantityAfter}</strong>
                <span className="subline">from {m.quantityBefore}</span>
              </td>
              <td className="reason-cell">{m.reason}</td>
              <td className="muted nowrap">{timestamp(m.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
export default function Movements() {
  const [searchParams, setSearchParams] = useSearchParams()
  const productId = searchParams.get('productId') ?? ''
  const [search, setSearch] = useState(''),
    [kind, setKind] = useState(''),
    [from, setFrom] = useState(''),
    [to, setTo] = useState(''),
    [page, setPage] = useState(1)
  const query = params({
    productId,
    search,
    kind,
    from: from ? `${from}T00:00:00Z` : '',
    to: to ? `${to}T23:59:59.999Z` : '',
    page,
    pageSize: 10,
  })
  const history = useQuery({
    queryKey: ['movements', query],
    queryFn: () => api<Page<Movement>>(`/stock/movements?${query}`),
  })
  const filter = (change: () => void) => {
    change()
    setPage(1)
  }
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">EVERY UNIT ACCOUNTED FOR</p>
          <h1>Stock movements</h1>
          <p className="muted">A permanent record of receipts, dispatches and adjustments.</p>
        </div>
        <span className="history-note">Immutable history</span>
      </div>
      <section className="panel">
        <div className="toolbar">
          <label className="search">
            <Search size={18} />
            <span className="sr-only">Search movements</span>
            <input
              value={search}
              maxLength={160}
              placeholder="Search product, SKU or reason…"
              onChange={(e) => filter(() => setSearch(e.target.value))}
            />
          </label>
          <label>
            <span className="sr-only">Movement filter</span>
            <select value={kind} onChange={(e) => filter(() => setKind(e.target.value))}>
              <option value="">All movements</option>
              <option value="in">Stock in</option>
              <option value="out">Stock out</option>
              <option value="adjustment">Adjustment</option>
            </select>
          </label>
          <label className="date-label">
            From (UTC)
            <input type="date" value={from} onChange={(e) => filter(() => setFrom(e.target.value))} />
          </label>
          <label className="date-label">
            To (UTC)
            <input type="date" value={to} onChange={(e) => filter(() => setTo(e.target.value))} />
          </label>
        </div>
        {productId && (
          <div className="table-subhead">
            Showing one product
            <button
              className="text-button"
              onClick={() => {
                setSearchParams({})
                setPage(1)
              }}
            >
              Show all products
            </button>
          </div>
        )}
        <ErrorMessage error={history.error} />
        {history.isPending ? (
          <Loading />
        ) : history.data?.items.length === 0 ? (
          <Empty title="No movements found">
            Receive stock or adjust a count from the product catalog. Try different filters to find earlier
            records.
          </Empty>
        ) : (
          history.data && <MovementTable movements={history.data.items} />
        )}{' '}
        {history.data && <Pagination page={page} total={history.data.total} onChange={setPage} />}
      </section>
      <p className="table-footnote">
        Dates use your local time. Date filters use UTC. Historical product details stay as recorded.
      </p>
    </>
  )
}
