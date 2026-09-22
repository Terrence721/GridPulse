import { Link } from 'react-router-dom'

function Landing() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-slate-100 px-6">
      <div className="max-w-md w-full flex flex-col items-center text-center gap-3">
        <h1 className="text-4xl font-bold text-slate-900 tracking-tight">GridPulse</h1>
        <p className="text-slate-500">Grid Operations Console</p>
        <div className="flex gap-3 mt-6">
          <Link
            to="/work-orders"
            className="bg-primary hover:bg-primary-hover text-white rounded-lg px-5 py-2.5 text-sm font-semibold transition-colors"
          >
            Work Orders
          </Link>
          <Link
            to="/outages"
            className="bg-white border border-slate-200 hover:border-slate-300 text-slate-700 rounded-lg px-5 py-2.5 text-sm font-semibold transition-colors"
          >
            Outages
          </Link>
        </div>
      </div>
    </div>
  )
}

export default Landing
