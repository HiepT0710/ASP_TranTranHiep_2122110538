import { useEffect, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useNotifications } from "../context/NotificationContext";
import { useAuth } from "../context/AuthContext";

const labels = {
  success: "Mới",
  info: "Cập nhật",
  error: "Hủy",
};

export default function NotificationBell() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { items, unreadCount, open, setOpen, clearNotifications, markRead, markAllRead } = useNotifications();
  const audience = user?.role?.toLowerCase();
  const latest = useMemo(() => items.filter((item) => !item.audience || item.audience === audience).slice(0, 8), [items, audience]);

  useEffect(() => {
    document.title = unreadCount > 0 ? `(${unreadCount}) FoodOrder Platform` : "FoodOrder Platform";
  }, [unreadCount]);

  return (
    <div className="notification-wrap">
      <button type="button" className="notification-bell secondary icon-btn" onClick={() => setOpen((v) => !v)}>
        <span>🔔</span>
        {unreadCount > 0 && <span className="notification-count">{unreadCount > 9 ? "9+" : unreadCount}</span>}
      </button>
      {open && (
        <div className="notification-panel">
          <div className="row" style={{ justifyContent: "space-between" }}>
            <b>Thông báo</b>
            <div className="row" style={{ margin: 0 }}>
              <button type="button" className="link-btn" onClick={markAllRead}>Đánh dấu đã đọc</button>
              <button type="button" className="link-btn" onClick={clearNotifications}>Xóa tất cả</button>
            </div>
          </div>
          {latest.length === 0 ? (
            <p className="muted">Chưa có thông báo mới.</p>
          ) : (
            latest.map((item) => (
              <div key={item.id} className={`notification-item notification-${item.type || "info"}`} onClick={() => {
                markRead(item.id);
                if (item.orderId) navigate(`/orders/${item.orderId}`);
              }} style={{ cursor: item.orderId ? "pointer" : "default" }}>
                <div className="row" style={{ justifyContent: "space-between", margin: 0 }}>
                  <b>{item.title}</b>
                  <span className="badge">{labels[item.type] || "Thông báo"}</span>
                </div>
                <p>{item.message}</p>
                <div className="muted" style={{ fontSize: 12 }}>{item.read ? "Đã đọc" : "Chưa đọc"}</div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
