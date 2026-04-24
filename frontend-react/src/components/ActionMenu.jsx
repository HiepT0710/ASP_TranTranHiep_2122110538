import { useEffect, useLayoutEffect, useRef, useState } from "react";

export default function ActionMenu({ label = "Actions", items = [] }) {
  const [open, setOpen] = useState(false);
  const [placement, setPlacement] = useState("down");
  const ref = useRef(null);

  useLayoutEffect(() => {
    if (!open || !ref.current) return;
    const rect = ref.current.getBoundingClientRect();
    const spaceBelow = window.innerHeight - rect.bottom;
    setPlacement(spaceBelow < 220 ? "up" : "down");
  }, [open]);

  useEffect(() => {
    const onDocClick = (event) => {
      if (ref.current && !ref.current.contains(event.target)) setOpen(false);
    };
    document.addEventListener("click", onDocClick);
    return () => document.removeEventListener("click", onDocClick);
  }, []);

  return (
    <div className={`action-menu ${placement === "up" ? "action-menu-up" : ""}`} ref={ref}>
      <button type="button" className="secondary" onClick={() => setOpen((v) => !v)} aria-expanded={open}>
        {label} ▾
      </button>
      {open && (
        <div className={`action-menu-panel ${placement === "up" ? "action-menu-panel-up" : ""}`}>
          {items.map((item) => (
            <button
              key={item.label}
              type="button"
              className={item.variant || "ghost"}
              onClick={() => {
                setOpen(false);
                item.onClick?.();
              }}
            >
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
