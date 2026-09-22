import { useState } from 'react'
import { useGetWorkOrdersQuery, useLazyGeocodeAddressQuery } from '../../app/gatewayApi.ts'
import MapModal from './MapModal.tsx'

function WorkOrdersList() {
  const { data: workOrders, isLoading, error } = useGetWorkOrdersQuery()
  const [geocodeAddress] = useLazyGeocodeAddressQuery()
  const [mapLocation, setMapLocation] = useState<{ latitude: number; longitude: number } | null>(null)

  async function showOnMap(streetNumber: number | null, streetName: string | null) {
    try {
      const result = await geocodeAddress({ streetNumber, streetName }).unwrap()
      setMapLocation(result)
    } catch {
      alert('Could not locate that address on the map.')
    }
  }

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
                {wo.streetNumber && wo.streetName && wo.streetName !== 'Unknown' ? (
                  <button
                    onClick={() => showOnMap(wo.streetNumber, wo.streetName)}
                    className="text-primary hover:underline"
                  >
                    {wo.streetNumber} {wo.streetName}
                  </button>
                ) : (
                  <span className="text-slate-400">Location unknown</span>
                )}
              </td>
              <td className="px-4 py-3 text-slate-700">{wo.assignedCrew ?? '—'}</td>
              <td className="px-4 py-3 text-slate-500">{new Date(wo.createdAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {mapLocation && (
        <MapModal
          latitude={mapLocation.latitude}
          longitude={mapLocation.longitude}
          onClose={() => setMapLocation(null)}
        />
      )}
    </div>
  )
}

export default WorkOrdersList
