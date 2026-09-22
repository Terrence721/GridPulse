import { useAuth } from 'react-oidc-context'
import { Outlet } from 'react-router-dom'

function RequireRole({ role }: { role: string }) {
  const auth = useAuth()

  if (auth.user?.profile.role !== role) {
    return (
      <div className="max-w-md mx-auto mt-16 text-center">
        <h2 className="text-lg font-semibold text-slate-900">Access denied</h2>
        <p className="mt-2 text-sm text-slate-500">This page is only available to dispatchers.</p>
      </div>
    )
  }

  return <Outlet />
}

export default RequireRole
