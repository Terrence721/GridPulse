import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import { AuthProvider } from 'react-oidc-context'
import { store } from './app/store.ts'
import { oidcConfig } from './auth/oidcConfig.ts'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider
      {...oidcConfig}
      onSigninCallback={() => {
        window.history.replaceState({}, document.title, window.location.pathname)
      }}
    >
      <Provider store={store}>
        <App />
      </Provider>
    </AuthProvider>
  </StrictMode>,
)
