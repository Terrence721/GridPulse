import { useAuth } from 'react-oidc-context'
import { useGetMeQuery } from './app/gatewayApi.ts'

function App() {
  const auth = useAuth()
  const { data: me, error, isLoading: meLoading } = useGetMeQuery(undefined, {
    skip: !auth.isAuthenticated,
  })

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
      <h2>Gateway check (/api/me)</h2>
      {meLoading && <p>Loading gateway response...</p>}
      {error && <p>Gateway error: {JSON.stringify(error)}</p>}
      {me && (
        <div>
          <p>Gateway says: {me.name}</p>
          <ul>
            {me.claims.map((c) => (
              <li key={c.type}>{c.type}: {c.value}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

export default App
