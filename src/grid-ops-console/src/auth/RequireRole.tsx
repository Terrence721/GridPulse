import { useAuth } from 'react-oidc-context'
import { Outlet, Navigate } from 'react-router-dom'

function RequireRole({ role }: { role: string }) {
  const auth = useAuth()

  if (auth.user?.profile.role !== role) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}

export default RequireRole
