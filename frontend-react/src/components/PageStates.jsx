export function SkeletonCardGrid({ count = 4 }) {
  return (
    <div className="cards" aria-busy="true" aria-live="polite">
      {Array.from({ length: count }).map((_, index) => (
        <div key={index} className="panel">
          <div className="skeleton skeleton-card" />
          <div className="skeleton skeleton-line" style={{ width: "72%", marginTop: 14 }} />
          <div className="skeleton skeleton-line" style={{ width: "48%" }} />
          <div className="skeleton skeleton-line" style={{ width: "86%" }} />
        </div>
      ))}
    </div>
  );
}

export function SkeletonTable({ rows = 4, cols = 5 }) {
  return (
    <div className="panel" aria-busy="true" aria-live="polite">
      {Array.from({ length: rows }).map((_, row) => (
        <div key={row} className="row" style={{ marginBottom: 12 }}>
          {Array.from({ length: cols }).map((__, col) => (
            <div key={col} className="skeleton skeleton-line" style={{ flex: 1, height: 18 }} />
          ))}
        </div>
      ))}
    </div>
  );
}

export function StateMessage({ title, description, action }) {
  return (
    <div className="empty-state">
      <div>
        <h3>{title}</h3>
        {description && <p className="muted">{description}</p>}
        {action}
      </div>
    </div>
  );
}
