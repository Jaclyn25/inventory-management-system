import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowRight, ArrowUpRight, AlertTriangle, RefreshCw } from 'lucide-react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { api, count, kindLabel, money, timestamp, type Product, type Report } from './api'
import { Empty, ErrorMessage, Loading, StockBadge } from './ui'
import { StockForm } from './Forms'

const palette = [
  'oklch(49% .13 41)',
  'oklch(55% .055 155)',
  'oklch(68% .07 80)',
  'oklch(48% .045 250)',
  'oklch(72% .04 55)',
  'oklch(64% .05 310)',
]
export function Metrics({ data }: { data: Report }) {
  return (
    <dl className="metrics">
      <div>
        <dt>Total products</dt>
        <dd>{count(data.totalProducts)}</dd>
        <span>Active catalog items</span>
      </div>
      <div>
        <dt>Inventory units</dt>
        <dd>{count(data.totalUnits)}</dd>
        <span>Across all matching products</span>
      </div>
      <div>
        <dt>Inventory value</dt>
        <dd>{money(data.inventoryValue)}</dd>
        <span>At current cost · USD</span>
      </div>
      <div>
        <dt>Needs attention</dt>
        <dd className="attention-count">
          {data.lowStockProducts + data.outOfStockProducts}
          <AlertTriangle size={21} />
        </dd>
        <span>
          {data.lowStockProducts} low · {data.outOfStockProducts} out of stock
        </span>
      </div>
    </dl>
  )
}
export function CategoryChart({ data }: { data: Report }) {
  return (
    <section className="panel distribution">
      <div className="section-heading">
        <div>
          <h2>Category distribution</h2>
          <p className="section-caption">Available units by category</p>
        </div>
        <span className="small-label">{data.categories.length} categories</span>
      </div>
      {data.totalUnits === 0 ? (
        <Empty title="No units to distribute">Receive stock to see your category mix.</Empty>
      ) : (
        <>
          <div
            className="donut-wrap"
            role="img"
            aria-label={`Category distribution: ${data.categories.map((c) => `${c.name}, ${c.units} units`).join('; ')}`}
          >
            <ResponsiveContainer width="100%" height={205} initialDimension={{ width: 350, height: 205 }}>
              <PieChart>
                <Pie
                  data={data.categories}
                  dataKey="units"
                  nameKey="name"
                  innerRadius={62}
                  outerRadius={86}
                  paddingAngle={3}
                  stroke="none"
                  isAnimationActive={false}
                >
                  {data.categories.map((c, i) => (
                    <Cell key={c.categoryId} fill={palette[i % palette.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
            <div className="donut-center">
              <strong>{count(data.totalUnits)}</strong>
              <span>total units</span>
            </div>
          </div>
          <ul className="chart-legend">
            {data.categories.map((c, i) => (
              <li key={c.categoryId}>
                <span className="legend-dot" style={{ background: palette[i % palette.length] }} />
                <span>{c.name}</span>
                <strong>{count(c.units)}</strong>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  )
}
export default function Dashboard({ notify }: { notify: (message: string) => void }) {
  const report = useQuery({
    queryKey: ['reports', 'dashboard'],
    queryFn: () => api<Report>('/reports'),
    refetchInterval: 30000,
  })
  const [moving, setMoving] = useState<Product | null>(null)
  const data = report.data
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">THE BIG PICTURE</p>
          <h1>Inventory overview</h1>
          <p className="muted">Know what’s on hand. See what needs your attention.</p>
        </div>
        <div className="heading-actions">
          <button
            className="icon-button"
            aria-label="Refresh dashboard"
            disabled={report.isFetching}
            onClick={() => report.refetch()}
          >
            <RefreshCw size={17} />
          </button>
          <Link className="button primary" to="/products">
            Manage inventory
            <ArrowUpRight size={17} />
          </Link>
        </div>
      </div>
      <ErrorMessage error={report.error} />
      {report.isPending ? (
        <Loading />
      ) : (
        data && (
          <>
            <Metrics data={data} />
            {data.lowStockProducts + data.outOfStockProducts > 0 && (
              <div className="attention-banner">
                <span className="alert-icon">
                  <AlertTriangle size={19} />
                </span>
                <div>
                  <strong>A little attention keeps things moving.</strong>
                  <p>
                    {data.lowStockProducts} products are running low and {data.outOfStockProducts} are out of
                    stock.
                  </p>
                </div>
                <Link to="/reports?status=out">
                  Review stock
                  <ArrowRight size={16} />
                </Link>
              </div>
            )}
            <div className="dashboard-charts">
              <section className="panel activity-panel">
                <div className="section-heading">
                  <div>
                    <h2>Stock activity</h2>
                    <p className="section-caption">Units received and issued over the last 14 days</p>
                  </div>
                  <span className="small-label">Daily · UTC</span>
                </div>
                <div className="activity-legend">
                  <span>
                    <i style={{ background: palette[0] }} />
                    Received
                  </span>
                  <span>
                    <i style={{ background: palette[3] }} />
                    Issued
                  </span>
                  <span>
                    <i style={{ background: palette[1] }} />
                    Adjusted
                  </span>
                </div>
                <div
                  className="activity-chart"
                  role="img"
                  aria-label="Daily stock movement chart. Tabular activity is available in Reports."
                >
                  <ResponsiveContainer
                    width="100%"
                    height={240}
                    initialDimension={{ width: 600, height: 240 }}
                  >
                    <BarChart
                      data={data.activity}
                      margin={{ top: 15, right: 18, bottom: 0, left: -18 }}
                      barGap={2}
                    >
                      <CartesianGrid stroke="oklch(92% .01 70)" vertical={false} />
                      <XAxis
                        dataKey="date"
                        tickFormatter={(value) => String(value).slice(5).replace('-', '/')}
                        axisLine={false}
                        tickLine={false}
                        tick={{ fontSize: 10, fill: 'oklch(48% .015 65)' }}
                        minTickGap={18}
                      />
                      <YAxis
                        axisLine={false}
                        tickLine={false}
                        tick={{ fontSize: 10, fill: 'oklch(48% .015 65)' }}
                        allowDecimals={false}
                      />
                      <Tooltip cursor={{ fill: 'oklch(96% .01 70)' }} />
                      <Bar
                        name="Received"
                        dataKey="received"
                        fill={palette[0]}
                        radius={[3, 3, 0, 0]}
                        isAnimationActive={false}
                      />
                      <Bar
                        name="Issued"
                        dataKey="issued"
                        fill={palette[3]}
                        radius={[3, 3, 0, 0]}
                        isAnimationActive={false}
                      />
                      <Bar
                        name="Adjusted"
                        dataKey="adjusted"
                        fill={palette[1]}
                        radius={[3, 3, 0, 0]}
                        isAnimationActive={false}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
                <div className="chart-bottom">
                  <span>Every movement is recorded in your stock ledger.</span>
                  <Link to="/movements">
                    View all movements
                    <ArrowRight size={14} />
                  </Link>
                </div>
              </section>
              <CategoryChart data={data} />
            </div>
            <div className="dashboard-bottom">
              <section className="panel">
                <div className="section-heading">
                  <div>
                    <h2>Replenishment watchlist</h2>
                    <p className="section-caption">Out-of-stock and low-stock products first</p>
                  </div>
                  <Link className="text-button" to="/reports?status=low">
                    View report
                    <ArrowRight size={14} />
                  </Link>
                </div>
                {data.attentionProducts.length === 0 ? (
                  <Empty title="Stock is in good shape">There are no low-stock products to review.</Empty>
                ) : (
                  <div className="table-scroll" tabIndex={0} aria-label="Products needing attention">
                    <table>
                      <thead>
                        <tr>
                          <th>Product</th>
                          <th className="numeric">On hand</th>
                          <th>Status</th>
                          <th>
                            <span className="sr-only">Action</span>
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.attentionProducts.map((p) => (
                          <tr key={p.id}>
                            <td>
                              <strong>{p.name}</strong>
                              <span className="subline mono">{p.sku}</span>
                            </td>
                            <td className="numeric">
                              <strong>{p.quantity}</strong>
                              <span className="subline">Min. {p.lowStockThreshold}</span>
                            </td>
                            <td>
                              <StockBadge product={p} />
                            </td>
                            <td>
                              <button
                                className="icon-button"
                                aria-label={`Receive stock for ${p.name}`}
                                onClick={() => setMoving(p)}
                              >
                                <ArrowUpRight size={18} />
                              </button>
                            </td>
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
                    <h2>Recent movements</h2>
                    <p className="section-caption">The latest changes to your stock</p>
                  </div>
                  <Link className="text-button" to="/movements">
                    View all
                    <ArrowRight size={14} />
                  </Link>
                </div>
                {data.recentMovements.length === 0 ? (
                  <Empty title="Your ledger starts here">
                    Receive stock from the product catalog to record your first movement.
                  </Empty>
                ) : (
                  <ul className="recent-list">
                    {data.recentMovements.slice(0, 6).map((m) => (
                      <li key={m.id}>
                        <span className={`recent-icon ${m.kind}`}>
                          <ArrowUpRight size={17} />
                        </span>
                        <div>
                          <strong>{m.productName}</strong>
                          <span>
                            {kindLabel(m.kind)} · {timestamp(m.createdAt)}
                          </span>
                        </div>
                        <strong className={m.delta > 0 ? 'positive' : 'negative'}>
                          {m.delta > 0 ? '+' : ''}
                          {m.delta}
                          <span>units</span>
                        </strong>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            </div>
            <p className="table-footnote">
              Snapshot: {timestamp(data.asOf)} · Auto-refreshes every 30 seconds · Valued at current cost
            </p>
          </>
        )
      )}
      {moving && <StockForm product={moving} onClose={() => setMoving(null)} onSaved={notify} />}
    </>
  )
}
