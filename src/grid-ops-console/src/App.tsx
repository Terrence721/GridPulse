import { useAuth } from 'react-oidc-context'

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
    </div>
  )
}

export default App
