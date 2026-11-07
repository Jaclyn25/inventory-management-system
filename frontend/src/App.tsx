import { useState, lazy, Suspense } from 'react'
import { NavLink, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { Boxes, Package, ArrowLeftRight, Folder, Menu, X, ShieldCheck, LayoutDashboard, ChartNoAxesCombined } from 'lucide-react'
import Products from './Products'
import Categories from './Categories'
import Movements from './Movements'
const Dashboard = lazy(() => import('./Dashboard'))
const Reports = lazy(() => import('./Reports'))
import { Loading } from './ui'

export default function App() {
  const [menu, setMenu] = useState(false), [message, setMessage] = useState('')
  const location = useLocation()
  const notify = (text: string) => setMessage(text)
  return <div className="app-shell"><a className="skip-link" href="#main">Skip to content</a>{menu && <button className="nav-backdrop" aria-label="Close navigation" onClick={() => setMenu(false)}/>}
    <aside id="primary-sidebar" className={`sidebar ${menu ? 'open' : ''}`}><a href="/" className="brand"><span className="brand-mark"><Boxes size={24}/></span>stockroom<span className="brand-dot">.</span></a><button className="mobile-close icon-button" aria-label="Close navigation" onClick={() => setMenu(false)}><X/></button><div className="workspace"><span className="workspace-avatar">N</span><div><strong>Northline Supply</strong><span>Inventory workspace</span></div></div><p className="nav-label">WORKSPACE</p><nav aria-label="Primary navigation"><NavLink to="/" end onClick={() => setMenu(false)}><LayoutDashboard size={19}/>Overview</NavLink><NavLink to="/products" onClick={() => setMenu(false)}><Package size={19}/>Products</NavLink><NavLink to="/movements" onClick={() => setMenu(false)}><ArrowLeftRight size={19}/>Stock movements</NavLink><NavLink to="/categories" onClick={() => setMenu(false)}><Folder size={19}/>Categories</NavLink><NavLink to="/reports" onClick={() => setMenu(false)}><ChartNoAxesCombined size={19}/>Reports</NavLink></nav><div className="sidebar-bottom"><ShieldCheck size={21}/><strong>Local workspace</strong><p>Keep inventory in good order.<br/>Every movement leaves a record.</p><span>USD · Trusted local use</span></div></aside>
    <div className="workspace-main"><header className="topbar"><div><button className="mobile-toggle icon-button" aria-label="Open navigation" aria-expanded={menu} aria-controls="primary-sidebar" onClick={() => setMenu(!menu)}><Menu size={22}/></button><span className="breadcrumb">Workspace <span>/</span> <strong>{({ '/': 'Overview', '/reports': 'Reports', '/products': 'Products', '/movements': 'Stock movements', '/categories': 'Categories' })[location.pathname] ?? 'Inventory'}</strong></span></div><div className="topbar-right"><span className="local-indicator">Local environment</span><span className="user-avatar" aria-label="Inventory operator">NS</span></div></header>
    <main id="main" tabIndex={-1}>{message && <div role="status" className="toast"><ShieldCheck size={18}/>{message}<button className="icon-button" aria-label="Dismiss notification" onClick={() => setMessage('')}><X size={16}/></button></div>}<Suspense fallback={<Loading/>}><Routes><Route path="/" element={<Dashboard notify={notify}/>}/><Route path="/reports" element={<Reports/>}/><Route path="/products" element={<Products notify={notify}/>}/><Route path="/categories" element={<Categories notify={notify}/>}/><Route path="/movements" element={<Movements/>}/><Route path="*" element={<Navigate to="/" replace/>}/></Routes></Suspense><footer>Stockroom <span>Clarity in every count.</span><span>Inventory workspace · USD</span></footer></main></div>
  </div>
}
