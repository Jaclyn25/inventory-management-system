import { useEffect, useId, useRef, type ReactNode } from 'react'
import { AlertCircle, X, PackageOpen, ArrowLeft, ArrowRight } from 'lucide-react'
import type { Product } from './api'

export function Dialog({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  useEffect(() => { const dialog = ref.current!; dialog.showModal(); return () => dialog.close() }, [])
  return <dialog ref={ref} aria-labelledby={titleId} onCancel={event => { event.preventDefault(); onClose() }}>
    <div className="dialog-head"><h2 id={titleId}>{title}</h2><button className="icon-button" aria-label="Close dialog" onClick={onClose}><X size={20}/></button></div>{children}
  </dialog>
}
export function ErrorMessage({ error }: { error: Error | null }) {
  return error ? <div role="alert" className="error-message"><AlertCircle size={18}/><span>{error.message}</span></div> : null
}
export function Loading() { return <div role="status" aria-label="Loading" className="loading"><div/><div/><div/><span className="sr-only">Loading inventory…</span></div> }
export function Empty({ title, children }: { title: string; children?: ReactNode }) { return <div className="empty"><PackageOpen size={34}/><h3>{title}</h3><p>{children || 'Try another search or change your filters.'}</p></div> }
export function StockBadge({ product }: { product: Product }) {
  const state = product.isArchived ? 'archived' : product.quantity === 0 ? 'out' : product.quantity <= product.lowStockThreshold ? 'low' : 'healthy'
  return <span className={`badge ${state}`}><span aria-hidden="true"/>{({ archived: 'Archived', out: 'Out of stock', low: 'Low stock', healthy: 'In stock' })[state]}</span>
}
export function Pagination({ page, total, size = 10, onChange }: { page: number; total: number; size?: number; onChange: (page: number) => void }) {
  return <div className="pagination"><span>{total === 0 ? '0 results' : `${(page - 1) * size + 1}–${Math.min(page * size, total)} of ${total} results`}</span><div><button aria-label="Previous page" disabled={page <= 1} onClick={() => onChange(page - 1)}><ArrowLeft size={16}/></button><span>Page {page} of {Math.max(1, Math.ceil(total / size))}</span><button aria-label="Next page" disabled={page * size >= total} onClick={() => onChange(page + 1)}><ArrowRight size={16}/></button></div></div>
}
