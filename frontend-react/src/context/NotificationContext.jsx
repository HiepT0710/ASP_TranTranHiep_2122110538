import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import { createOrderNotificationConnection, createOrderChatConnection } from "../services/apiService";
import { playNotifySound } from "../utils/notificationSound";
import { useAuth } from "./AuthContext";
import { useToast } from "./ToastContext";

const NotificationContext = createContext(null);

export function NotificationProvider({ children }) {
  const { user, isAuthenticated } = useAuth();
  const { pushToast } = useToast();
  const [items, setItems] = useState([]);
  const [open, setOpen] = useState(false);
  const connectionRef = useRef(null);
  const startedForUserRef = useRef(null);

  useEffect(() => {
    const stop = async () => {
      try {
        await connectionRef.current?.stop();
      } catch {
        // ignore
      }
      connectionRef.current = null;
      startedForUserRef.current = null;
    };

    const start = async () => {
      await stop();
      if (!isAuthenticated || !user?.id) return;
      if (startedForUserRef.current === String(user.id)) return;
      startedForUserRef.current = String(user.id);

      const notificationConnection = createOrderNotificationConnection();
      const chatConnection = createOrderChatConnection();
      connectionRef.current = notificationConnection;

      const role = user.role;
      const pushItem = (title, message, type, orderId, audience = role) => {
        const item = {
          id: `${audience}-${title}-${orderId}-${Date.now()}`,
          title,
          message,
          type,
          orderId,
          audience,
          read: false,
          createdAt: new Date().toISOString(),
        };
        setItems((current) => [item, ...current].slice(0, 30));
        playNotifySound();
      };

      const notifyRole = (targetRole, title, message, type, orderId) => {
        if (role !== targetRole) return;
        pushItem(title, message, type, orderId, targetRole.toLowerCase());
        pushToast(message, type);
      };

      notificationConnection.on("OrderStatusChanged", (payload) => {
        const orderId = payload.id || payload.Id;
        const status = payload.status || payload.Status || "Đã cập nhật";
        notifyRole("User", `Đơn #${orderId}`, `Đơn hàng vừa chuyển sang ${status}`, "info", orderId);
        notifyRole("Seller", `Đơn #${orderId}`, `Đơn hàng vừa chuyển sang ${status}`, "info", orderId);
        notifyRole("Admin", `Đơn #${orderId}`, `Đơn hàng vừa chuyển sang ${status}`, "info", orderId);
      });

      notificationConnection.on("OrderCreated", (payload) => {
        const orderId = payload.id || payload.Id;
        notifyRole("Seller", `Đơn mới #${orderId}`, `Có đơn hàng mới cần xử lý`, "success", orderId);
      });

      notificationConnection.on("OrderCancelled", (payload) => {
        const orderId = payload.id || payload.Id;
        notifyRole("User", `Đơn hủy #${orderId}`, `Đơn hàng đã bị hủy`, "error", orderId);
        notifyRole("Seller", `Đơn hủy #${orderId}`, `Đơn hàng đã bị hủy`, "error", orderId);
        notifyRole("Admin", `Đơn hủy #${orderId}`, `Đơn hàng đã bị hủy`, "error", orderId);
      });

      try {
        await notificationConnection.start();
        await chatConnection.start();
      } catch (error) {
        startedForUserRef.current = null;
        pushToast(error?.message || "Không thể kết nối thông báo realtime", "error");
      }
    };

    start();
    return () => {
      stop();
    };
  }, [isAuthenticated, user?.id, user?.role, pushToast]);

  const unreadCount = items.filter((item) => !item.read).length;

  const value = useMemo(
    () => ({
      items,
      unreadCount,
      open,
      setOpen,
      markAllRead: () => setItems((current) => current.map((item) => ({ ...item, read: true }))),
      markRead: (id) => setItems((current) => current.map((item) => (item.id === id ? { ...item, read: true } : item))),
      clearNotifications: () => setItems([]),
      pushLocalNotification: (notification) => setItems((current) => [notification, ...current].slice(0, 30)),
    }),
    [items, unreadCount, open]
  );

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
}

export function useNotifications() {
  const ctx = useContext(NotificationContext);
  if (!ctx) throw new Error("useNotifications must be used within NotificationProvider");
  return ctx;
}
