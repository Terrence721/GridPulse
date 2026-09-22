import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import { BrowserRouter } from 'react-router-dom'
import { store } from './app/store.ts'
import AuthRoot from './auth/AuthRoot.tsx'
import App from './App.tsx'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Provider store={store}>
        <AuthRoot>
          <App />
        </AuthRoot>
      </Provider>
    </BrowserRouter>
  </StrictMode>,
)
