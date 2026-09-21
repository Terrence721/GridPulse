import { useAuth } from 'react-oidc-context'
import { Routes, Route, Navigate, Link } from 'react-router-dom'
import WorkOrdersList from './features/work-orders/WorkOrdersList.tsx'

function App() {
  const auth = useAuth()

  if (auth.isLoading) {
    return <div>Loading...</div>
  }

  if (auth.error) {
    return <div>Authentication error: {auth.error.message}</div>
  }

  if (!auth.isAuthenticated) {
    return (
      <div>
        <h1>Grid Operations Console</h1>
        <button onClick={() => auth.signinRedirect()}>Log in</button>
      </div>
    )
  }

  return (
    <div>
      <h1>Grid Operations Console</h1>
      <p>Signed in as {auth.user?.profile.name ?? auth.user?.profile.sub}</p>
      <button onClick={() => auth.signoutRedirect()}>Log out</button>
      <nav>
        <Link to="/work-orders">Work Orders</Link>
      </nav>
      <Routes>
        <Route path="/" element={<Navigate to="/work-orders" replace />} />
        <Route path="/work-orders" element={<WorkOrdersList />} />
      </Routes>
    </div>
  )
}

export default App
