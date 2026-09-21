import { useAuth } from 'react-oidc-context'
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
      <h2>Work Orders</h2>
      <WorkOrdersList />
    </div>
  )
}

export default App
