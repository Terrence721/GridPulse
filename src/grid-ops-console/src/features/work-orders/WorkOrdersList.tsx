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
    <div className="bg-white rounded-lg border border-slate-200 overflow-hidden">
      <table className="w-full text-sm text-left">
        <thead className="bg-slate-50 text-slate-700 border-b border-slate-200">
          <tr>
            <th className="px-4 py-3 font-semibold">Hazard</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="px-4 py-3 font-semibold">Location</th>
            <th className="px-4 py-3 font-semibold">Assigned crew</th>
            <th className="px-4 py-3 font-semibold">Created</th>
          </tr>
        </thead>
        <tbody>
          {workOrders?.map((wo) => (
            <tr key={wo.id} className="border-b border-slate-200 last:border-0 hover:bg-slate-50">
              <td className="px-4 py-3 text-slate-900">{wo.hazardType}</td>
              <td className="px-4 py-3 text-slate-700">{wo.status}</td>
              <td className="px-4 py-3 text-slate-700">
                <a
                  href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${wo.streetNumber ?? ''} ${wo.streetName ?? ''}`)}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-primary hover:underline"
                >
                  {wo.streetNumber} {wo.streetName}
                </a>
              </td>
              <td className="px-4 py-3 text-slate-700">{wo.assignedCrew ?? '—'}</td>
              <td className="px-4 py-3 text-slate-500">{new Date(wo.createdAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default WorkOrdersList
