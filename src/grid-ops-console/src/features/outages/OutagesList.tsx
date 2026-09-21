import { useGetOutagesQuery } from '../../app/gatewayApi.ts'

function OutagesList() {
  const { data: outages, isLoading, error } = useGetOutagesQuery()

  if (isLoading) {
    return <p>Loading outages...</p>
  }

  if (error) {
    return <p>Failed to load outages: {JSON.stringify(error)}</p>
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Status</th>
          <th>Location</th>
          <th>Detected</th>
        </tr>
      </thead>
      <tbody>
        {outages?.map((o) => (
          <tr key={o.id}>
            <td>{o.status}</td>
            <td>{o.streetNumberRangeStart}–{o.streetNumberRangeEnd} {o.streetName}</td>
            <td>{new Date(o.detectedAt).toLocaleString()}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default OutagesList
