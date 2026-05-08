import { useState } from "react";
import ReportButton from "./ReportButton";

export default function ChatMiniPanel({ orderId, messages = [] }) {
  const [open, setOpen] = useState(false);

  return (
    <div className="panel" style={{ marginTop: 12 }}>
      <div className="row" style={{ justifyContent: "space-between" }}>
        <b>Chat đơn hàng</b>
        <button type="button" className="secondary" onClick={() => setOpen((v) => !v)}>{open ? "Ẩn" : "Hiện"}</button>
      </div>
      {open && (
        <>
          <div style={{ maxHeight: 220, overflow: "auto", marginTop: 10 }}>
            {messages.map((m) => (
              <div key={m.id} className="panel soft-panel" style={{ marginBottom: 8 }}>
                <b>{m.username || "User"}</b>
                <p style={{ marginBottom: 0 }}>{m.message}</p>
              </div>
            ))}
          </div>
          <ReportButton targetType="Chat" targetId={orderId} label="Báo cáo chat" />
        </>
      )}
    </div>
  );
}
