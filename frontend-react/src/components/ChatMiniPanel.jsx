import { useMemo, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { useChat } from "../context/ChatContext";

export default function ChatMiniPanel() {
  const { user } = useAuth();
  const {
    closeSupport,
    threads,
    selectedOrderId,
    selectedTarget,
    setSelectedTarget,
    hydrateThread,
    sendMessage,
    clearUnread,
    getThreadByOrder,
    openSupport,
  } = useChat();
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);
  const [loadingTarget, setLoadingTarget] = useState(false);

  const role = user?.role || "";
  const canSwitchTarget = role === "User" && selectedOrderId;
  const activeThread = selectedOrderId ? getThreadByOrder(selectedOrderId, selectedTarget) : null;
  const messages = activeThread?.messages || [];
  const availableThreads = useMemo(
    () => Object.values(threads || {}).filter((thread) => thread?.orderId && !thread?.ended).sort((a, b) => (b?.lastActivityAt || 0) - (a?.lastActivityAt || 0)),
    [threads]
  );

  const title = useMemo(() => {
    if (!selectedOrderId) return "Hỗ trợ chat";
    return `Chat đơn #${selectedOrderId}`;
  }, [selectedOrderId]);

  const switchTarget = async (target) => {
    if (!selectedOrderId || loadingTarget) return;
    setSelectedTarget(target);
    setLoadingTarget(true);
    try {
      await hydrateThread(selectedOrderId, target);
      clearUnread(selectedOrderId, target);
    } finally {
      setLoadingTarget(false);
    }
  };

  const submit = async () => {
    const text = input.trim();
    if (!text || !selectedOrderId) return;
    try {
      setSending(true);
      await sendMessage(selectedOrderId, text, selectedTarget);
      clearUnread(selectedOrderId, selectedTarget);
      setInput("");
    } finally {
      setSending(false);
    }
  };

  return (
    <aside className="messenger-drawer open">
      <div className="messenger-header">
        <div>
          <div className="messenger-title">{title}</div>
          <p className="muted" style={{ margin: "6px 0 0" }}>
            {selectedOrderId ? `Đơn #${selectedOrderId}` : "Chọn đơn hàng để bắt đầu hỗ trợ"}
          </p>
        </div>
        <button type="button" className="secondary" onClick={closeSupport}>Đóng</button>
      </div>

      {!selectedOrderId ? (
        <div className="messenger-conversation">
          {availableThreads.length === 0 ? (
            <p className="muted" style={{ margin: 0 }}>Chưa có cuộc chat nào. Vào đơn hàng và bấm Chat hoặc Liên hệ admin.</p>
          ) : (
            availableThreads.map((thread, idx) => (
              <button
                key={`${thread.orderId}-${thread.target || "seller"}-${idx}`}
                type="button"
                className="messenger-tab"
                onClick={() => openSupport(thread.orderId, thread.target || "seller")}
              >
                Đơn #{thread.orderId} · {thread.target === "admin" ? "admin" : "seller"}
              </button>
            ))
          )}
        </div>
      ) : (
        <>
          {canSwitchTarget && (
            <div className="messenger-tabs">
              <button type="button" className={selectedTarget === "seller" ? "" : "secondary"} onClick={() => switchTarget("seller")} disabled={loadingTarget}>
                Chat với seller
              </button>
              <button type="button" className={selectedTarget === "admin" ? "" : "secondary"} onClick={() => switchTarget("admin")} disabled={loadingTarget}>
                Liên hệ admin
              </button>
            </div>
          )}

          <div className="messenger-conversation">
            {messages.length === 0 ? (
              <p className="muted">Chưa có tin nhắn ở kênh {selectedTarget === "admin" ? "admin" : "seller"}.</p>
            ) : (
              messages.map((m, idx) => (
                <div key={m.id || idx} className="panel soft-panel" style={{ marginBottom: 8 }}>
                  <b>{m.username || "User"}</b>
                  <p style={{ marginBottom: 4 }}>{m.message}</p>
                  <span className="muted">{m.createdAt ? new Date(m.createdAt).toLocaleString() : ""}</span>
                </div>
              ))
            )}
          </div>

          <div className="messenger-compose">
            <input
              style={{ flex: 1 }}
              placeholder={selectedTarget === "admin" ? "Nhập nội dung cần admin hỗ trợ..." : "Nhập tin nhắn cho seller..."}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              disabled={sending}
            />
            <button type="button" className="secondary" onClick={submit} disabled={sending || !input.trim()}>
              Gửi
            </button>
          </div>
        </>
      )}
    </aside>
  );
}
