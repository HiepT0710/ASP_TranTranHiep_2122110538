import { useEffect } from "react";
import { useChat } from "../context/ChatContext";

export default function ChatBubble({ orderId, onOpen }) {
  const { threads, minimized, hydrateThread, clearUnread, reopenBubble, closeBubble, endConversation } = useChat();
  const key = String(orderId);
  const thread = threads[key] || { messages: [], unreadCount: 0, open: true, ended: false };
  const lastMessage = thread.messages[thread.messages.length - 1];

  useEffect(() => {
    hydrateThread(orderId);
  }, [orderId]);

  if (thread.ended) return null;

  if (minimized[key]) {
    return (
      <button type="button" className="chat-bubble minimized" onClick={() => reopenBubble(orderId)}>
        <span className="chat-bubble-title">Hỗ trợ chat</span>
        {thread.unreadCount > 0 && <span className="chat-bubble-count">{thread.unreadCount}</span>}
      </button>
    );
  }

  return (
    <div className="chat-bubble">
      <div className="chat-bubble-header">
        <div>
          <b>Hỗ trợ chat</b>
          <div className="muted">{lastMessage ? lastMessage.message : "Chưa có tin nhắn"}</div>
        </div>
        <div className="row" style={{ margin: 0 }}>
          <button type="button" className="link-btn" onClick={() => clearUnread(orderId)}>Đã đọc</button>
          <button type="button" className="link-btn" onClick={() => endConversation(orderId)}>Kết thúc</button>
          <button type="button" className="link-btn" onClick={() => closeBubble(orderId)}>Ẩn</button>
        </div>
      </div>
      <div className="chat-bubble-body">
        {(thread.messages || []).slice(-5).map((m, idx) => (
          <div key={m.id || idx} className={`chat-bubble-message ${m.mine ? "mine" : ""}`}>
            <b>{m.username || "Bạn"}</b>
            <p>{m.message || m.Message}</p>
          </div>
        ))}
      </div>
      <button type="button" className="secondary" onClick={onOpen}>Mở khung chat đầy đủ</button>
      {thread.unreadCount > 0 && <span className="chat-bubble-unread">{thread.unreadCount} tin chưa đọc</span>}
    </div>
  );
}
