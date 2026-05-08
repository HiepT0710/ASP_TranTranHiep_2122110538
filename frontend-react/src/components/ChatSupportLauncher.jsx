import { useChat } from "../context/ChatContext";

export default function ChatSupportLauncher() {
  const { openSupport, threads } = useChat();
  const unread = Object.values(threads).reduce((sum, thread) => sum + (thread?.unreadCount || 0), 0);

  return (
    <button type="button" className="support-fab secondary icon-btn" onClick={() => openSupport()} aria-label="Mở hỗ trợ chat">
      <span>💬 Hỗ trợ</span>
      {unread > 0 && <span className="support-fab-count">{unread > 9 ? "9+" : unread}</span>}
    </button>
  );
}
