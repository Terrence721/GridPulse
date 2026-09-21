import { useGetWorkOrdersQuery } from '../../app/gatewayApi.ts'

function WorkOrdersList() {
  const { data: workOrders, isLoading, error } = useGetWorkOrdersQuery()

  if (isLoading) {
    return <p>Loading work orders...</p>
  }

  if (error) {
    return <p>Failed to load work orders: {JSON.stringify(error)}</p>
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Hazard</th>
          <th>Status</th>
          <th>Location</th>
          <th>Assigned crew</th>
          <th>Created</th>
        </tr>
      </thead>
      <tbody>
        {workOrders?.map((wo) => (
          <tr key={wo.id}>
            <td>{wo.hazardType}</td>
            <td>{wo.status}</td>
            <td>{wo.streetNumber} {wo.streetName}</td>
            <td>{wo.assignedCrew ?? '—'}</td>
            <td>{new Date(wo.createdAt).toLocaleString()}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default WorkOrdersList
