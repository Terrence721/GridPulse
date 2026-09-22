import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import { AuthProvider } from 'react-oidc-context'
import { BrowserRouter } from 'react-router-dom'
import { store } from './app/store.ts'
import { oidcConfig } from './auth/oidcConfig.ts'
import App from './App.tsx'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider
      {...oidcConfig}
      onSigninCallback={(user) => {
        window.history.replaceState({}, document.title, (user?.state as string | undefined) ?? '/')
      }}
    >
      <Provider store={store}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </Provider>
    </AuthProvider>
  </StrictMode>,
)
