interface TrafficChartProps {
  /** Requests per second, oldest → newest. */
  data: number[]
}

/** Dependency-free SVG area/line chart. Color follows the current text color (dark-aware). */
export function TrafficChart({ data }: TrafficChartProps) {
  const width = 600
  const height = 80
  const max = Math.max(1, ...data)
  const stepX = data.length > 1 ? width / (data.length - 1) : width

  const points = data.map((v, i) => `${(i * stepX).toFixed(1)},${(height - (v / max) * height).toFixed(1)}`)
  const line = points.join(' ')
  const area = `0,${height} ${line} ${width},${height}`

  return (
    <svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none" className="w-full h-20 text-blue-500 dark:text-blue-400">
      {data.length > 0 && <polygon points={area} fill="currentColor" opacity="0.15" />}
      {data.length > 1 && (
        <polyline points={line} fill="none" stroke="currentColor" strokeWidth="2" vectorEffect="non-scaling-stroke" />
      )}
    </svg>
  )
}
