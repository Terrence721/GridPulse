import { useParams } from 'react-router-dom'
import { useGetWorkOrderQuery } from '../../app/gatewayApi.ts'

function WorkOrderDetail() {
  const { id } = useParams<{ id: string }>()
  const { data, isLoading, error } = useGetWorkOrderQuery(id!, { skip: !id })

  if (isLoading) {
    return <p>Loading work order...</p>
  }

  if (error) {
    return <p>Failed to load work order: {JSON.stringify(error)}</p>
  }

  if (!data) {
    return <p>Work order not found.</p>
  }

  const { workOrder, outage } = data

  return (
    <div>
      <h3>{workOrder.hazardType}</h3>
      <dl>
        <dt>Status</dt>
        <dd>{workOrder.status}</dd>
        <dt>Location</dt>
        <dd>{workOrder.streetNumber} {workOrder.streetName}</dd>
        <dt>Description</dt>
        <dd>{workOrder.description}</dd>
        <dt>Assigned crew</dt>
        <dd>{workOrder.assignedCrew ?? '—'}</dd>
        <dt>Created</dt>
        <dd>{new Date(workOrder.createdAt).toLocaleString()}</dd>
        <dt>Last updated</dt>
        <dd>{new Date(workOrder.updatedAt).toLocaleString()}</dd>
      </dl>
      {outage && (
        <div>
          <h4>Linked outage</h4>
          <dl>
            <dt>Status</dt>
            <dd>{outage.status}</dd>
            <dt>Location</dt>
            <dd>{outage.streetNumberRangeStart}–{outage.streetNumberRangeEnd} {outage.streetName}</dd>
            <dt>Detected</dt>
            <dd>{new Date(outage.detectedAt).toLocaleString()}</dd>
          </dl>
        </div>
      )}
    </div>
  )
}

export default WorkOrderDetail
