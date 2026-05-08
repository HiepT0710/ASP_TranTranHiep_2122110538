import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useToast } from "../context/ToastContext";

export default function ActionMenu({ label = "Actions", items = [] }) {
  const { pushToast } = useToast();
  const [open, setOpen] = useState(false);
  const [placement, setPlacement] = useState("down");
  const [coords, setCoords] = useState({ top: 0, left: 0, width: 0 });
  const ref = useRef(null);

  useLayoutEffect(() => {
    if (!open || !ref.current) return;
    const rect = ref.current.getBoundingClientRect();
    const spaceBelow = window.innerHeight - rect.bottom;
    const isUp = spaceBelow < 240;
    setPlacement(isUp ? "up" : "down");
    setCoords({
      top: isUp ? rect.top : rect.bottom + 8,
      left: rect.right,
      width: rect.width,
    });
  }, [open]);

  useEffect(() => {
    const onDocClick = (event) => {
      if (ref.current && !ref.current.contains(event.target)) setOpen(false);
    };
    const onScroll = () => setOpen(false);
    document.addEventListener("click", onDocClick);
    window.addEventListener("scroll", onScroll, true);
    return () => {
      document.removeEventListener("click", onDocClick);
      window.removeEventListener("scroll", onScroll, true);
    };
  }, []);

  const panel = useMemo(() => {
    if (!open) return null;
    const panelStyle = placement === "up"
      ? { position: "fixed", top: Math.max(8, coords.top - 8), left: Math.max(8, coords.left - 190), minWidth: 190, zIndex: 100000 }
      : { position: "fixed", top: Math.max(8, coords.top), left: Math.max(8, coords.left - 190), minWidth: 190, zIndex: 100000 };

    return createPortal(
      <div className="action-menu-panel" style={panelStyle}>
        {items.map((item) => (
          <button
            key={item.label}
            type="button"
            className={item.variant || "ghost"}
            onClick={() => {
              setOpen(false);
              pushToast(item.toast || `${item.label} đã được chọn`, item.toastType || "info");
              item.onClick?.();
            }}
          >
            {item.label}
          </button>
        ))}
      </div>,
      document.body
    );
  }, [coords.left, coords.top, items, open, placement, pushToast]);

  return (
    <div className="action-menu" ref={ref}>
      <button type="button" className="secondary" onClick={() => setOpen((v) => !v)} aria-expanded={open}>
        {label} ▾
      </button>
      {panel}
    </div>
  );
}
