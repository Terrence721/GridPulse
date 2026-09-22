interface MapModalProps {
  latitude: number
  longitude: number
  onClose: () => void
}

function MapModal({ latitude, longitude, onClose }: MapModalProps) {
  return (
    <div
      className="fixed inset-0 bg-slate-900/50 flex items-center justify-center p-6 z-50"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-lg border border-slate-200 shadow-lg w-full max-w-2xl overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-4 py-3 border-b border-slate-200">
          <span className="text-sm font-semibold text-slate-900">Location</span>
          <button onClick={onClose} className="text-slate-500 hover:text-slate-700 text-sm font-semibold">
            Close
          </button>
        </div>
        <iframe
          title="Location map"
          className="w-full h-96 border-0"
          src={`https://www.google.com/maps?q=${latitude},${longitude}&output=embed`}
        />
      </div>
    </div>
  )
}

export default MapModal
