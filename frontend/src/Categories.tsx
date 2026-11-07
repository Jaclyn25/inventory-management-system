import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Folder, Pencil, Trash2 } from 'lucide-react'
import { api, json, type Category } from './api'
import { Dialog, Empty, ErrorMessage, Loading } from './ui'

const schema = z.object({ name: z.string().trim().min(1, 'Category name is required.').max(80) })
export default function Categories({ notify }: { notify: (message: string) => void }) {
  const query = useQuery({ queryKey: ['categories'], queryFn: () => api<Category[]>('/categories') })
  const client = useQueryClient()
  const [editing, setEditing] = useState<Category | null>(null),
    [deleting, setDeleting] = useState<Category | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { name: '' } })
  const save = useMutation({
    mutationFn: (values: z.infer<typeof schema>) =>
      api(`/categories${editing ? `/${editing.id}` : ''}`, json(editing ? 'PUT' : 'POST', values)),
    onSuccess: async () => {
      await client.invalidateQueries()
      setEditing(null)
      reset({ name: '' })
      notify('Category saved.')
    },
  })
  const remove = useMutation({
    mutationFn: (id: string) => api(`/categories/${id}`, { method: 'DELETE' }),
    onSuccess: async () => {
      await client.invalidateQueries()
      setDeleting(null)
      notify('Category deleted.')
    },
  })
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">CATALOG STRUCTURE</p>
          <h1>Categories</h1>
          <p className="muted">A clear place for every product.</p>
        </div>
      </div>
      <div className="category-layout">
        <section className="panel">
          <div className="section-heading">
            <h2>Your categories</h2>
            <span className="muted">{query.data?.length ?? 0} groups</span>
          </div>
          <ErrorMessage error={query.error} />
          {query.isPending ? (
            <Loading />
          ) : query.data?.length === 0 ? (
            <Empty title="Create your first category">
              Use categories to organize products and filter reports.
            </Empty>
          ) : (
            <ul className="category-list">
              {query.data?.map((c) => (
                <li key={c.id}>
                  <span className="category-icon">
                    <Folder size={19} />
                  </span>
                  <strong>{c.name}</strong>
                  <div className="row-actions">
                    <button
                      className="icon-button"
                      aria-label={`Rename ${c.name}`}
                      onClick={() => {
                        setEditing(c)
                        reset({ name: c.name })
                        save.reset()
                      }}
                    >
                      <Pencil size={17} />
                    </button>
                    <button
                      className="icon-button"
                      aria-label={`Delete ${c.name}`}
                      onClick={() => {
                        setDeleting(c)
                        remove.reset()
                      }}
                    >
                      <Trash2 size={17} />
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="panel category-form">
          <h2>{editing ? 'Rename category' : 'New category'}</h2>
          <p className="muted">Names must be unique. Categories used by products cannot be deleted.</p>
          <form onSubmit={handleSubmit((values) => save.mutate(values))} noValidate>
            <ErrorMessage error={save.error} />
            <label>
              Category name
              <input {...register('name')} aria-invalid={!!errors.name} maxLength={80} />
              {errors.name && <span className="field-error">{errors.name.message}</span>}
            </label>
            <div className="dialog-actions">
              {editing && (
                <button
                  type="button"
                  onClick={() => {
                    setEditing(null)
                    reset({ name: '' })
                    save.reset()
                  }}
                >
                  Cancel
                </button>
              )}
              <button className="primary" disabled={save.isPending}>
                {save.isPending ? 'Saving…' : editing ? 'Save category' : 'Add category'}
              </button>
            </div>
          </form>
        </section>
      </div>
      {deleting && (
        <Dialog title="Delete category?" onClose={() => setDeleting(null)}>
          <p className="description">
            Delete <strong>{deleting.name}</strong>? This is available only when no products reference this
            category.
          </p>
          <ErrorMessage error={remove.error} />
          <div className="dialog-actions">
            <button disabled={remove.isPending} onClick={() => setDeleting(null)}>
              Cancel
            </button>
            <button className="danger" disabled={remove.isPending} onClick={() => remove.mutate(deleting.id)}>
              Confirm delete
            </button>
          </div>
        </Dialog>
      )}
    </>
  )
}
