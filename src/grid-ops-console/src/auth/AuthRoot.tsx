import type { ReactNode } from 'react'
import { AuthProvider } from 'react-oidc-context'
import { useNavigate } from 'react-router-dom'
import { oidcConfig } from './oidcConfig.ts'

function AuthRoot({ children }: { children: ReactNode }) {
  const navigate = useNavigate()

  return (
    <AuthProvider
      {...oidcConfig}
      onSigninCallback={(user) => {
        navigate((user?.state as string | undefined) ?? '/', { replace: true })
      }}
    >
      {children}
    </AuthProvider>
  )
}

export default AuthRoot
