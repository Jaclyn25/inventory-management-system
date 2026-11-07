import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, ApiError, json, kindLabel, type Category, type Product } from './api'
import { Dialog, ErrorMessage, Loading } from './ui'

const price = z.string().regex(/^\d{1,9}(\.\d{1,2})?$/, 'Use a price with up to 2 decimal places.')
const productSchema = z.object({
  sku: z
    .string()
    .regex(/^[A-Za-z0-9][A-Za-z0-9_-]{1,39}$/, 'Use 2–40 letters, numbers, hyphens or underscores.'),
  name: z.string().trim().min(1, 'Product name is required.').max(160),
  description: z.string().max(2000),
  categoryId: z.string().min(1, 'Choose a category.'),
  unitPrice: price,
  costPrice: price,
  lowStockThreshold: z
    .string()
    .regex(/^\d+$/, 'Use a whole number.')
    .refine((v) => Number(v) <= 1000000000, 'Maximum is 1,000,000,000.'),
})
type ProductFields = z.infer<typeof productSchema>
export function ProductForm({
  product,
  onClose,
  onSaved,
}: {
  product?: Product
  onClose: () => void
  onSaved: (message: string) => void
}) {
  const client = useQueryClient()
  const categories = useQuery({ queryKey: ['categories'], queryFn: () => api<Category[]>('/categories') })
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProductFields>({
    resolver: zodResolver(productSchema),
    defaultValues: product
      ? { ...product, lowStockThreshold: String(product.lowStockThreshold) }
      : {
          sku: '',
          name: '',
          description: '',
          categoryId: '',
          unitPrice: '',
          costPrice: '',
          lowStockThreshold: '10',
        },
  })
  const mutation = useMutation({
    mutationFn: (values: ProductFields) =>
      api(
        `/products${product ? `/${product.id}` : ''}`,
        json(product ? 'PUT' : 'POST', {
          ...values,
          lowStockThreshold: Number(values.lowStockThreshold),
          version: product?.version ?? 1,
        }),
      ),
    onSuccess: async () => {
      await client.invalidateQueries()
      onSaved(product ? 'Product updated.' : 'Product created. Receive stock to add its first units.')
      onClose()
    },
  })
  const field = (name: keyof ProductFields, label: string, type = 'text') => (
    <label>
      {label}
      <input
        {...register(name)}
        type={type}
        aria-invalid={!!errors[name]}
        inputMode={name.includes('Price') ? 'decimal' : undefined}
      />
      {errors[name] && <span className="field-error">{errors[name]?.message}</span>}
    </label>
  )
  return (
    <Dialog title={product ? 'Edit product' : 'Add product'} onClose={onClose}>
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} noValidate>
        <p className="form-intro">
          {product
            ? 'Update catalog details. Stock changes are recorded separately.'
            : 'Start with the details. New products begin with zero stock.'}
        </p>
        <ErrorMessage error={mutation.error || categories.error} />
        <fieldset disabled={mutation.isPending}>
          <div className="form-grid">
            {field('name', 'Product name')}
            {field('sku', 'SKU')}
            <label className="span-2">
              Description
              <textarea rows={3} {...register('description')} />
              {errors.description && <span className="field-error">{errors.description.message}</span>}
            </label>
            <label className="span-2">
              Category
              <select {...register('categoryId')} aria-invalid={!!errors.categoryId}>
                <option value="">Select category</option>
                {categories.data?.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
              {errors.categoryId && <span className="field-error">{errors.categoryId.message}</span>}
            </label>
            {field('costPrice', 'Cost price (USD)')}
            {field('unitPrice', 'Unit price (USD)')}
            {field('lowStockThreshold', 'Low-stock threshold')}
          </div>
        </fieldset>
        <div className="dialog-actions">
          <button type="button" onClick={onClose} disabled={mutation.isPending}>
            Cancel
          </button>
          <button className="primary" disabled={mutation.isPending || categories.isPending}>
            {mutation.isPending ? 'Saving…' : 'Save product'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}

const stockSchema = z.object({
  quantity: z
    .string()
    .regex(/^\d+$/, 'Use a whole number.')
    .refine((v) => Number(v) <= 1000000000, 'Maximum is 1,000,000,000.'),
  reason: z.string().trim().min(3, 'Add a reason with at least 3 characters.').max(500),
})
type StockFields = z.infer<typeof stockSchema>
export function StockForm({
  product: initial,
  onClose,
  onSaved,
}: {
  product: Product
  onClose: () => void
  onSaved: (message: string) => void
}) {
  const client = useQueryClient()
  const [kind, setKind] = useState('in')
  const [uncertain, setUncertain] = useState(false)
  const draft = useRef<{ signature: string; operationId: string; key: string } | null>(null)
  const productQuery = useQuery({
    queryKey: ['product', initial.id],
    queryFn: () => api<Product>(`/products/${initial.id}`),
  })
  const product = productQuery.data ?? initial
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<StockFields>({
    resolver: zodResolver(stockSchema),
    defaultValues: { quantity: '', reason: '' },
  })
  const mutation = useMutation({
    mutationFn: async (values: StockFields) => {
      if (uncertain && draft.current) {
        const { kind: savedKind, ...saved } = JSON.parse(draft.current.signature)
        return api(`/stock/${savedKind}`, {
          ...json('POST', { ...saved, operationId: draft.current.operationId }),
          headers: { 'Idempotency-Key': draft.current.key },
        })
      }
      const payload = {
        productId: product.id,
        quantity: Number(values.quantity),
        reason: values.reason,
        ...(kind === 'adjustment' ? { version: product.version } : {}),
      }
      const signature = JSON.stringify({ kind, ...payload })
      if (!draft.current || draft.current.signature !== signature)
        draft.current = { signature, operationId: crypto.randomUUID(), key: crypto.randomUUID() }
      return api(`/stock/${kind}`, {
        ...json('POST', { ...payload, operationId: draft.current.operationId }),
        headers: { 'Idempotency-Key': draft.current.key },
      })
    },
    onSuccess: async () => {
      await client.invalidateQueries()
      onSaved(`${kindLabel(kind)} recorded.`)
      onClose()
    },
    onError: (error) => {
      setUncertain(!(error instanceof ApiError))
    },
  })
  function submit(values: StockFields) {
    if (kind !== 'adjustment' && Number(values.quantity) === 0) {
      setError('quantity', { message: 'Quantity must be greater than zero.' })
      return
    }
    if (kind === 'out' && Number(values.quantity) > product.quantity) {
      setError('quantity', { message: 'Quantity exceeds available stock.' })
      return
    }
    mutation.mutate(values)
  }
  return (
    <Dialog
      title="Record stock movement"
      onClose={() => {
        if (!uncertain && !mutation.isPending) onClose()
      }}
    >
      <div className="stock-context">
        <div>
          <strong>{product.name}</strong>
          <span className="muted mono">{product.sku}</span>
        </div>
        <div>
          <strong>{product.quantity}</strong>
          <span className="muted">units available</span>
        </div>
      </div>
      {productQuery.isPending ? (
        <Loading />
      ) : (
        <form onSubmit={handleSubmit(submit)} noValidate>
          <ErrorMessage error={mutation.error || productQuery.error} />
          {uncertain && (
            <p role="status" className="notice">
              The connection was interrupted. Retry this unchanged operation to confirm its result safely.
            </p>
          )}
          <fieldset disabled={mutation.isPending || uncertain || !!productQuery.error}>
            <div className="form-grid">
              <label className="span-2">
                Movement type
                <select value={kind} onChange={(e) => setKind(e.target.value)}>
                  <option value="in">Stock in</option>
                  <option value="out">Stock out</option>
                  <option value="adjustment">Adjustment</option>
                </select>
              </label>
              <label className="span-2">
                {kind === 'adjustment' ? 'Counted quantity' : 'Quantity'}
                <input inputMode="numeric" {...register('quantity')} aria-invalid={!!errors.quantity} />
                {errors.quantity && <span className="field-error">{errors.quantity.message}</span>}
              </label>
              <label className="span-2">
                Reason
                <textarea
                  rows={3}
                  placeholder={
                    kind === 'adjustment'
                      ? 'Explain the count discrepancy…'
                      : 'Delivery reference, dispatch or other context…'
                  }
                  {...register('reason')}
                  aria-invalid={!!errors.reason}
                />
                {errors.reason && <span className="field-error">{errors.reason.message}</span>}
              </label>
            </div>
          </fieldset>
          {kind === 'adjustment' && (
            <p className="form-intro">
              Enter the actual count. The difference is recorded as an adjustment; stale counts are rejected.
            </p>
          )}
          <div className="dialog-actions">
            <button type="button" onClick={onClose} disabled={mutation.isPending || uncertain}>
              Cancel
            </button>
            <button className="primary" disabled={mutation.isPending || !!productQuery.error}>
              {mutation.isPending ? 'Recording…' : uncertain ? 'Retry same operation' : 'Confirm movement'}
            </button>
          </div>
        </form>
      )}
    </Dialog>
  )
}
