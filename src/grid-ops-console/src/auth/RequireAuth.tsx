import { useEffect } from 'react'
import { useAuth } from 'react-oidc-context'
import { Outlet, useLocation } from 'react-router-dom'

function RequireAuth() {
  const auth = useAuth()
  const location = useLocation()

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      auth.signinRedirect({ state: location.pathname })
    }
  }, [auth, location])

  if (!auth.isAuthenticated) {
    return <div>Redirecting to login...</div>
  }

  return <Outlet />
}

export default RequireAuth
