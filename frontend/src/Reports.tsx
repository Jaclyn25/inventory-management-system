import { useState } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Search, ArrowUpRight } from 'lucide-react'
import {
  api,
  count,
  money,
  params,
  timestamp,
  type Category,
  type Page,
  type Product,
  type Report,
} from './api'
import { Metrics } from './Dashboard'
import { Empty, ErrorMessage, Loading, Pagination, StockBadge } from './ui'

export default function Reports() {
  const [searchParams] = useSearchParams()
  const [search, setSearch] = useState(''),
    [categoryId, setCategory] = useState(''),
    [status, setStatus] = useState(searchParams.get('status') || 'all'),
    [page, setPage] = useState(1)
  const filter = params({ search, categoryId, status })
  const report = useQuery({ queryKey: ['reports', filter], queryFn: () => api<Report>(`/reports?${filter}`) })
  const products = useQuery({
    queryKey: ['products', 'report', filter, page],
    queryFn: () =>
      api<Page<Product>>(`/products?${filter}&page=${page}&pageSize=10&sort=value&descending=true`),
  })
  const categories = useQuery({ queryKey: ['categories'], queryFn: () => api<Category[]>('/categories') })
  const change = (fn: () => void) => {
    fn()
    setPage(1)
  }
  const data = report.data
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">A CLOSER LOOK</p>
          <h1>Inventory reports</h1>
          <p className="muted">Understand stock health, distribution and current-cost valuation.</p>
        </div>
        <Link className="button" to="/movements">
          Movement history
          <ArrowUpRight size={16} />
        </Link>
      </div>
      <div className="report-filters panel">
        <label className="search">
          <Search size={18} />
          <span className="sr-only">Search report</span>
          <input
            value={search}
            maxLength={160}
            placeholder="Search products or SKUs…"
            onChange={(e) => change(() => setSearch(e.target.value))}
          />
        </label>
        <label>
          <span className="sr-only">Report category</span>
          <select value={categoryId} onChange={(e) => change(() => setCategory(e.target.value))}>
            <option value="">All categories</option>
            {categories.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span className="sr-only">Report stock status</span>
          <select value={status} onChange={(e) => change(() => setStatus(e.target.value))}>
            <option value="all">All stock levels</option>
            <option value="low">Low stock</option>
            <option value="out">Out of stock</option>
            <option value="healthy">In stock</option>
          </select>
        </label>
      </div>
      <ErrorMessage error={report.error || products.error || categories.error} />
      {report.isPending ? (
        <Loading />
      ) : (
        data && (
          <>
            <Metrics data={data} />
            <section className="panel">
              <div className="section-heading">
                <div>
                  <h2>Inventory valuation</h2>
                  <p className="section-caption">Filtered products, ordered by cost value</p>
                </div>
                <span className="small-label">USD</span>
              </div>
              {products.isPending ? (
                <Loading />
              ) : products.data?.items.length === 0 ? (
                <Empty title="No products match this report" />
              ) : (
                <div className="table-scroll" tabIndex={0} aria-label="Inventory valuation table">
                  <table>
                    <thead>
                      <tr>
                        <th>Product</th>
                        <th>Category</th>
                        <th className="numeric">Quantity</th>
                        <th className="numeric">Cost price</th>
                        <th className="numeric">Cost value</th>
                        <th>Status</th>
                        <th>History</th>
                      </tr>
                    </thead>
                    <tbody>
                      {products.data?.items.map((p) => (
                        <tr key={p.id}>
                          <td>
                            <strong>{p.name}</strong>
                            <span className="subline mono">{p.sku}</span>
                          </td>
                          <td className="muted">{p.categoryName}</td>
                          <td className="numeric">{count(p.quantity)}</td>
                          <td className="numeric">{money(p.costPrice)}</td>
                          <td className="numeric">
                            <strong>{money(p.inventoryValue)}</strong>
                          </td>
                          <td>
                            <StockBadge product={p} />
                          </td>
                          <td>
                            <Link
                              className="text-button"
                              aria-label={`History for ${p.name}`}
                              to={`/movements?productId=${p.id}`}
                            >
                              View ledger
                              <ArrowUpRight size={14} />
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              {products.data && <Pagination page={page} total={products.data.total} onChange={setPage} />}
              <div className="report-total">
                <span>Current-cost inventory value</span>
                <strong data-testid="report-value">{money(data.inventoryValue)}</strong>
                <span>Retail value: {money(data.retailValue)}</span>
              </div>
            </section>
            <div className="reports-detail">
              <section className="panel">
                <div className="section-heading">
                  <h2>Category breakdown</h2>
                </div>
                {data.categories.length === 0 ? (
                  <Empty title="No category data" />
                ) : (
                  <div className="table-scroll" tabIndex={0} aria-label="Category valuation">
                    <table>
                      <thead>
                        <tr>
                          <th>Category</th>
                          <th className="numeric">Products</th>
                          <th className="numeric">Units</th>
                          <th className="numeric">Cost value</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.categories.map((c) => (
                          <tr key={c.categoryId}>
                            <td>{c.name}</td>
                            <td className="numeric">{c.products}</td>
                            <td className="numeric">{count(c.units)}</td>
                            <td className="numeric">{money(c.value)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
              <section className="panel">
                <div className="section-heading">
                  <div>
                    <h2>Daily movement totals</h2>
                    <p className="section-caption">Last 14 days · UTC</p>
                  </div>
                </div>
                <div className="table-scroll activity-table" tabIndex={0} aria-label="Daily stock activity">
                  <table>
                    <thead>
                      <tr>
                        <th>Date</th>
                        <th className="numeric">Received</th>
                        <th className="numeric">Issued</th>
                        <th className="numeric">Adjusted</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.activity
                        .slice()
                        .reverse()
                        .map((day) => (
                          <tr key={day.date}>
                            <td>{day.date}</td>
                            <td className="numeric">{day.received}</td>
                            <td className="numeric">{day.issued}</td>
                            <td className="numeric">
                              {day.adjusted > 0 ? '+' : ''}
                              {day.adjusted}
                            </td>
                          </tr>
                        ))}
                    </tbody>
                  </table>
                </div>
              </section>
            </div>
            <p className="table-footnote">
              Snapshot: {timestamp(data.asOf)} · Inventory value uses current cost price.
            </p>
          </>
        )
      )}
    </>
  )
}
