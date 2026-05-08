import { useEffect, useMemo, useState } from "react";
import { useChat } from "../context/ChatContext";

export default function ChatPanel({ orderId, enabled = true }) {
  const { hydrateThread, threads, sendMessage, clearUnread, endConversation, closeBubble } = useChat();
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);

  useEffect(() => {
    if (enabled) hydrateThread(orderId);
  }, [orderId, enabled, hydrateThread]);

  const thread = threads[String(orderId)] || { messages: [], unreadCount: 0, open: true, ended: false, loaded: false };
  const messages = thread.messages || [];

  const statusText = useMemo(() => {
    if (!enabled) return "Không khả dụng";
    return thread.ended ? "Đã kết thúc" : "Đang hoạt động";
  }, [enabled, thread.ended]);

  const submit = async () => {
    const text = input.trim();
    if (!text) return;
    try {
      setSending(true);
      await sendMessage(orderId, text);
      clearUnread(orderId);
      setInput("");
    } finally {
      setSending(false);
    }
  };

  return (
    <section className="panel" id="chat-panel-full">
      <div className="row" style={{ justifyContent: "space-between" }}>
        <h3 style={{ margin: 0 }}>Hỗ trợ realtime</h3>
        <span className="badge">{statusText}</span>
      </div>
      <p className="muted">Chat với admin hoặc seller khi cần hỗ trợ món ăn, đơn hàng hoặc lỗi hệ thống.</p>
      <div className="row" style={{ marginTop: 8 }}>
        <button type="button" className="secondary" onClick={() => closeBubble(orderId)}>Ẩn bong bóng</button>
        <button type="button" className="secondary" onClick={() => endConversation(orderId)}>Kết thúc trò chuyện</button>
      </div>
      <div className="chat-box" style={{ maxHeight: 320 }}>
        {messages.length === 0 ? (
          <p className="muted">Chưa có tin nhắn hỗ trợ.</p>
        ) : (
          messages.map((m, idx) => (
            <div key={m.id || idx} className={`chat-item ${m.mine ? "mine" : ""}`}>
              <b>{m.username || "Bạn"}</b>: {m.message}
              <div className="muted">{m.createdAt}</div>
            </div>
          ))
        )}
      </div>
      <div className="row" style={{ marginTop: 12 }}>
        <input
          style={{ flex: 1 }}
          placeholder="Nhập tin nhắn hỗ trợ..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={!enabled || thread.ended}
        />
        <button className="secondary" onClick={submit} disabled={!enabled || sending || thread.ended}>
          Gửi
        </button>
      </div>
    </section>
  );
}
