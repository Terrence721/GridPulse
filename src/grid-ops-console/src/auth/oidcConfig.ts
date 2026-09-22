import type { AuthProviderNoUserManagerProps } from 'react-oidc-context'

export const oidcConfig: AuthProviderNoUserManagerProps = {
  authority: 'https://localhost:7105',
  client_id: 'grid-ops-console',
  redirect_uri: 'http://localhost:5107/callback',
  post_logout_redirect_uri: 'http://localhost:5107/',
  scope: 'openid profile roles grid-ops-api',
  loadUserInfo: true,
}
