export default function SortFilterBar({
  filters = [],
  sortOptions = [],
  value,
  onChange,
  className = "",
}) {
  return (
    <div className={`panel ${className} sort-filter-bar`.trim()}>
      <div className="sort-filter-bar__filters">
        {filters.map((filter) => (
          <div key={filter.key} className="sort-filter-bar__field" style={{ minWidth: filter.minWidth || 180 }}>
            {filter.type === "select" ? (
              <select
                value={value[filter.key] ?? ""}
                onChange={(e) => onChange({ ...value, [filter.key]: e.target.value })}
                style={{ width: "100%" }}
              >
                {filter.options?.map((option) => (
                  <option key={option.value ?? "all"} value={option.value ?? ""}>{option.label}</option>
                ))}
              </select>
            ) : (
              <input
                placeholder={filter.placeholder || filter.label}
                value={value[filter.key] ?? ""}
                onChange={(e) => onChange({ ...value, [filter.key]: e.target.value })}
                style={{ width: "100%" }}
              />
            )}
          </div>
        ))}
      </div>

      {sortOptions.length > 0 && (
        <div className="sort-filter-bar__sort">
          <select
            value={value.sortBy ?? ""}
            onChange={(e) => onChange({ ...value, sortBy: e.target.value })}
            style={{ width: "100%" }}
          >
            {sortOptions.map((option) => (
              <option key={option.value ?? "default"} value={option.value ?? ""}>{option.label}</option>
            ))}
          </select>
        </div>
      )}
    </div>
  );
}
