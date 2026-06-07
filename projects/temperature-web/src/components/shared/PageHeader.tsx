interface PageHeaderProps {
  title: string
  children?: React.ReactNode
}

export function PageHeader({ title, children }: PageHeaderProps) {
  return (
    <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
      <h1 style={{ fontSize: '24px', fontWeight: 700, color: '#111827', margin: 0 }}>{title}</h1>
      {children && <div>{children}</div>}
    </header>
  )
}
