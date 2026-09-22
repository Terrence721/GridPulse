import { useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { Outlet, useLocation, Link } from 'react-router-dom'

function RequireAuth() {
  const auth = useAuth()
  const location = useLocation()

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated && !auth.error) {
      auth.signinRedirect({ state: location.pathname })
    }
  }, [auth, location])

  if (auth.error) {
    return <div>Authentication error: {auth.error.message}</div>
  }

  if (!auth.isAuthenticated) {
    return <div>Redirecting to login...</div>
  }

  return (
    <div className="min-h-screen bg-slate-100">
      <header className="bg-white border-b border-slate-200 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <span className="font-bold text-slate-900">GridPulse</span>
          <nav className="flex gap-4">
            {auth.user?.profile.role === 'dispatcher' && (
              <Link to="/work-orders" className="text-sm font-semibold text-slate-700">Work Orders</Link>
            )}
            <Link to="/outages" className="text-sm font-semibold text-slate-700">Outages</Link>
          </nav>
        </div>
        <div className="flex items-center gap-4">
          <span className="text-sm text-slate-500">{auth.user?.profile.name ?? auth.user?.profile.sub}</span>
          <button
            onClick={() => auth.signoutRedirect()}
            className="bg-primary hover:bg-primary-hover text-white rounded-lg px-4 py-2 text-sm font-semibold transition-colors"
          >
            Log out
          </button>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}

export default RequireAuth
