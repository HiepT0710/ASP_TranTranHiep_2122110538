import { useEffect, useMemo, useState } from "react";
import { useChat } from "../context/ChatContext";

export default function ChatFloatingBubble() {
  const { threads, minimized, reopenBubble, closeBubble, endConversation, clearUnread } = useChat();
  const [expandedThreadKey, setExpandedThreadKey] = useState(null);

  const activeThreads = useMemo(
    () => Object.entries(threads)
      .filter(([, thread]) => (thread?.messages?.length || thread?.unreadCount > 0 || thread?.open) && !thread.ended)
      .sort((a, b) => (b[1].lastActivityAt || 0) - (a[1].lastActivityAt || 0)),
    [threads]
  );

  useEffect(() => {
    const saved = localStorage.getItem("chat-bubbles-state");
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        if (parsed?.expandedThreadKey != null) setExpandedThreadKey(String(parsed.expandedThreadKey));
      } catch {
        localStorage.removeItem("chat-bubbles-state");
      }
    }
  }, []);

  useEffect(() => {
    localStorage.setItem("chat-bubbles-state", JSON.stringify({ expandedThreadKey }));
  }, [expandedThreadKey]);

  if (activeThreads.length === 0) return null;

  return (
    <div className="chat-bubbles-stack">
      {activeThreads.map(([threadKey, thread], index) => {
        const orderId = String(thread.orderId || threadKey.split("::")[0] || "");
        const target = thread.target || "seller";
        const lastMessage = thread.messages[thread.messages.length - 1];
        const isOpen = minimized[threadKey] ? false : expandedThreadKey === String(threadKey);
        const offset = index * 18;
        const style = {
          bottom: `${18 + offset}px`,
          right: `${18 + offset * 0.2}px`,
          transform: isOpen ? "translateY(0) scale(1)" : `translateY(${index * 6}px) scale(${1 - index * 0.02})`,
          zIndex: 10030 - index,
          background: "rgba(255,255,255,.98)",
          borderColor: "rgba(148,163,184,.18)",
        };

        return (
          <div key={threadKey} className={`chat-bubble-card ${isOpen ? "open" : "closed"}`} style={style}>
            <button
              type="button"
              className="chat-bubble-card-head"
              onClick={() => {
                if (isOpen) {
                  closeBubble(orderId, target);
                  setExpandedThreadKey(null);
                } else {
                  reopenBubble(orderId, target);
                  setExpandedThreadKey(String(threadKey));
                }
              }}
            >
              <div>
                <b>{thread.partnerName || lastMessage?.username || "Hỗ trợ"}</b>
                <div className="muted chat-bubble-status">Đơn #{orderId} · {target === "admin" ? "admin" : "seller"} · {thread.agentStatus || "đang chờ hỗ trợ"}</div>
              </div>
              <div className="chat-bubble-meta">
                {thread.unreadCount > 0 && <span className="chat-bubble-count">{thread.unreadCount}</span>}
                <span className={`chat-bubble-dot ${thread.online ? "online" : "offline"}`} />
              </div>
            </button>

            <div className={`chat-bubble-card-body ${isOpen ? "open" : "closed"}`}>
              <div className="chat-bubble-preview" onClick={() => setExpandedThreadKey(String(threadKey))} role="button" tabIndex={0}>
                <div className="chat-bubble-preview-top">
                  <span className={`chat-role-pill ${thread.online ? "online" : thread.agentStatus === "đang trả lời" ? "replying" : "away"}`}>
                    {thread.online ? "Online" : thread.agentStatus || "Away"}
                  </span>
                  <span className="muted">{lastMessage?.createdAt ? new Date(lastMessage.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : ""}</span>
                </div>
                <b>{lastMessage?.username || thread.partnerName || "Hỗ trợ"}</b>
                <p>{lastMessage?.message || "Chưa có tin nhắn"}</p>
              </div>

              <div className="chat-bubble-actions">
                <button type="button" className="secondary" onClick={() => clearUnread(orderId, target)}>Đã đọc</button>
                <button type="button" className="secondary" onClick={() => endConversation(orderId, target)}>Kết thúc</button>
                <button type="button" className="secondary" onClick={() => { closeBubble(orderId, target); setExpandedThreadKey(null); }}>Ẩn</button>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
