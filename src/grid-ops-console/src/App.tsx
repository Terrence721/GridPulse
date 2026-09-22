import { Routes, Route } from 'react-router-dom'
import Landing from './Landing.tsx'
import RequireAuth from './auth/RequireAuth.tsx'
import RequireRole from './auth/RequireRole.tsx'
import WorkOrdersList from './features/work-orders/WorkOrdersList.tsx'
import WorkOrderDetail from './features/work-orders/WorkOrderDetail.tsx'
import OutagesList from './features/outages/OutagesList.tsx'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/callback" element={<div>Signing in...</div>} />
      <Route element={<RequireAuth />}>
        <Route element={<RequireRole role="dispatcher" />}>
          <Route path="/work-orders" element={<WorkOrdersList />} />
          <Route path="/work-orders/:id" element={<WorkOrderDetail />} />
        </Route>
        <Route path="/outages" element={<OutagesList />} />
      </Route>
    </Routes>
  )
}

export default App
