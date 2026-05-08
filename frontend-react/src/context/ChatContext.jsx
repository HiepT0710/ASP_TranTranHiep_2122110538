import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { createOrderChatConnection, getOrderChatMessages, sendOrderChatMessage } from "../services/apiService";
import { useAuth } from "./AuthContext";
import { useToast } from "./ToastContext";

const ChatContext = createContext(null);
const STORAGE_KEY = "chat-ui-state-v2";

function loadSavedState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return { threads: {}, minimized: {}, selectedOrderId: null };
    const parsed = JSON.parse(raw);
    return {
      threads: parsed.threads || {},
      minimized: parsed.minimized || {},
      selectedOrderId: parsed.selectedOrderId ?? null,
    };
  } catch {
    return { threads: {}, minimized: {}, selectedOrderId: null };
  }
}

function saveState(threads, minimized, selectedOrderId, selectedTarget) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ threads, minimized, selectedOrderId, selectedTarget }));
  } catch {
    // ignore
  }
}

function normalizeChatMessage(message, fallbackOrderId, currentUserId) {
  if (!message || typeof message !== "object") return null;
  const rawOrderId = message.orderId ?? message.OrderId ?? fallbackOrderId;
  if (rawOrderId == null) return null;
  const text = String(message.message ?? message.Message ?? "");
  const createdAt = message.createdAt ?? message.CreatedAt ?? new Date().toISOString();
  return {
    id: message.id ?? message.Id ?? `${String(rawOrderId)}-${createdAt}-${String(message.userId ?? message.UserId ?? "")}`,
    orderId: rawOrderId,
    userId: message.userId ?? message.UserId ?? null,
    username: message.username ?? message.Username ?? "",
    message: text,
    createdAt,
    mine: String(message.userId ?? message.UserId ?? "") === String(currentUserId),
    ended: /\/(end|kết thúc)\b/i.test(text),
  };
}

function buildThreadDefaults(target = "seller") {
  return {
    messages: [],
    unreadCount: 0,
    open: true,
    ended: false,
    loaded: false,
    online: false,
    agentStatus: "đang chờ hỗ trợ",
    lastActivityAt: 0,
    partnerName: target === "admin" ? "Admin hỗ trợ" : "Seller hỗ trợ",
    target,
  };
}

function messageKey(message) {
  if (!message) return "";
  const createdAt = message.createdAt ? new Date(message.createdAt) : null;
  const minuteBucket = createdAt && !Number.isNaN(createdAt.getTime()) ? Math.floor(createdAt.getTime() / 60000) : "";
  const normalizedText = String(message.message ?? "").trim().toLowerCase();
  return `${String(message.orderId)}|${String(message.userId ?? "")}|${normalizedText}|${minuteBucket}`;
}

function mergeMessages(existingMessages, incomingMessages) {
  const seen = new Set(existingMessages.map(messageKey));
  const merged = [...existingMessages];
  for (const msg of incomingMessages) {
    const key = messageKey(msg);
    if (!key || seen.has(key)) continue;
    seen.add(key);
    merged.push(msg);
  }
  return merged.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
}

function restoreThread(thread) {
  return {
    ...buildThreadDefaults(thread?.target || "seller"),
    ...thread,
    messages: Array.isArray(thread?.messages) ? thread.messages : [],
  };
}

export function ChatProvider({ children }) {
  const { user, isAuthenticated } = useAuth();
  const { pushToast } = useToast();
  const saved = loadSavedState();
  const [threads, setThreads] = useState(() => saved.threads || {});
  const [minimized, setMinimized] = useState(() => saved.minimized || {});
  const [supportOpen, setSupportOpen] = useState(false);
  const [selectedOrderId, setSelectedOrderId] = useState(() => saved.selectedOrderId);
  const [selectedTarget, setSelectedTarget] = useState(() => saved.selectedTarget || "seller");
  const connectionRef = useRef(null);
  const connectingRef = useRef(false);
  const ownEchoIgnoreRef = useRef(new Map());

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ threads, minimized, selectedOrderId, selectedTarget }));
    } catch {
      // ignore
    }
  }, [threads, minimized, selectedOrderId, selectedTarget]);

  useEffect(() => {
    let cancelled = false;

    const stop = async () => {
      try {
        await connectionRef.current?.stop();
      } catch {
        // ignore
      }
      connectionRef.current = null;
      connectingRef.current = false;
    };

    const start = async () => {
      await stop();
      if (cancelled || !isAuthenticated || !user?.id) return;
      if (connectingRef.current) return;
      connectingRef.current = true;

      const connection = createOrderChatConnection();
      connectionRef.current = connection;

      connection.on("OrderMessageReceived", (payload) => {
        const normalized = normalizeChatMessage(payload, payload?.orderId ?? payload?.OrderId, user.id);
        if (!normalized) return;
        const key = String(normalized.orderId);
        const echoKey = `${key}|${String(normalized.userId ?? "")}|${String(normalized.message ?? "")}`;
        const ignoreUntil = ownEchoIgnoreRef.current.get(echoKey);
        if (normalized.mine && ignoreUntil && Date.now() < ignoreUntil) {
          ownEchoIgnoreRef.current.delete(echoKey);
          return;
        }
        if (normalized.mine) {
          ownEchoIgnoreRef.current.delete(echoKey);
        }
        setThreads((current) => {
          const thread = current[key] || buildThreadDefaults();
          const partnerName = normalized.mine ? thread.partnerName : (normalized.username || thread.partnerName || "Hỗ trợ");
          const messages = mergeMessages(thread.messages, [normalized]);
          return {
            ...current,
            [key]: {
              ...thread,
              messages,
              unreadCount: normalized.mine ? thread.unreadCount : thread.unreadCount + 1,
              open: true,
              ended: thread.ended || normalized.ended,
              loaded: true,
              lastActivityAt: Date.now(),
              online: true,
              agentStatus: normalized.mine ? thread.agentStatus : "đang trả lời",
              partnerName,
            },
          };
        });
        setMinimized((current) => ({ ...current, [key]: false }));
        if (!normalized.mine) {
          setSelectedOrderId(key);
          setSupportOpen(true);
          void hydrateThread(key);
          pushToast(normalized.message || "Có tin nhắn hỗ trợ mới", "info");
        }
      });

      try {
        await connection.start();
      } catch (error) {
        connectingRef.current = false;
        connectionRef.current = null;
        if (!cancelled) pushToast(error?.message || "Không thể kết nối chat realtime", "error");
        return;
      }

      connectingRef.current = false;
    };

    start();
    return () => {
      cancelled = true;
      stop();
    };
  }, [isAuthenticated, user?.id, pushToast]);

  const upsertThread = useCallback((orderId, updater) => {
    const key = String(orderId);
    setThreads((current) => {
      const existing = restoreThread(current[key]);
      const next = updater(existing);
      return { ...current, [key]: next };
    });
  }, []);

  const hydrateThread = useCallback(async (orderId) => {
    const key = String(orderId);
    let alreadyLoaded = false;
    setThreads((current) => {
      const existing = current[key];
      alreadyLoaded = !!existing?.loaded;
      return {
        ...current,
        [key]: existing || buildThreadDefaults(),
      };
    });
    if (alreadyLoaded) return;
    try {
      const history = await getOrderChatMessages(orderId, { page: 1, pageSize: 100 });
      const messages = (history.items || [])
        .map((item) => normalizeChatMessage(item, orderId, user?.id))
        .filter(Boolean);
      setThreads((current) => {
        const existing = restoreThread(current[key]);
        return {
          ...current,
          [key]: {
            ...existing,
            messages: mergeMessages(existing.messages, messages),
            loaded: true,
            lastActivityAt: messages.length ? Date.now() : existing.lastActivityAt,
          },
        };
      });
    } catch {
      // ignore
    }
  }, [user?.id]);

  const sendMessage = useCallback(async (orderId, message) => {
    const key = String(orderId);
    const optimistic = normalizeChatMessage({ orderId, userId: user?.id, username: user?.fullName || user?.username || "Bạn", message, createdAt: new Date().toISOString() }, orderId, user?.id);
    if (optimistic) {
      setThreads((current) => {
        const thread = restoreThread(current[key]);
        const messages = mergeMessages(thread.messages, [optimistic]);
        return {
          ...current,
          [key]: {
            ...thread,
            messages,
            unreadCount: thread.unreadCount,
            open: true,
            ended: thread.ended,
            loaded: true,
            lastActivityAt: Date.now(),
            online: true,
            agentStatus: thread.agentStatus || "đang trả lời",
            partnerName: thread.partnerName || "Hỗ trợ",
          },
        };
      });
      setMinimized((current) => ({ ...current, [key]: false }));
      ownEchoIgnoreRef.current.set(`${key}|${String(user?.id ?? "")}|${String(message)}`, Date.now() + 5000);
    }
    await sendOrderChatMessage(connectionRef.current, orderId, message);
  }, [user?.id, user?.fullName, user?.username]);

  const clearUnread = useCallback((orderId) => {
    upsertThread(orderId, (thread) => ({ ...thread, unreadCount: 0 }));
  }, [upsertThread]);

  const closeBubble = useCallback((orderId) => {
    const key = String(orderId);
    setMinimized((current) => ({ ...current, [key]: true }));
    upsertThread(orderId, (thread) => ({ ...thread, open: false }));
  }, [upsertThread]);

  const reopenBubble = useCallback((orderId) => {
    const key = String(orderId);
    setMinimized((current) => ({ ...current, [key]: false }));
    upsertThread(orderId, (thread) => ({ ...thread, open: true }));
  }, [upsertThread]);

  const endConversation = useCallback((orderId) => {
    const key = String(orderId);
    setMinimized((current) => ({ ...current, [key]: true }));
    upsertThread(orderId, (thread) => ({ ...thread, open: false, ended: true, agentStatus: "kết thúc" }));
  }, [upsertThread]);

  const openSupport = useCallback((orderId = null) => {
    setSelectedOrderId(orderId != null ? String(orderId) : null);
    setSupportOpen(true);
    if (orderId != null) hydrateThread(orderId).catch(() => {});
  }, [hydrateThread]);
  const closeSupport = useCallback(() => setSupportOpen(false), []);

  const value = useMemo(
    () => ({
      threads,
      minimized,
      supportOpen,
      selectedOrderId,
      hydrateThread,
      sendMessage,
      clearUnread,
      closeBubble,
      reopenBubble,
      endConversation,
      openSupport,
      closeSupport,
      getUnreadCount: (orderId) => threads[String(orderId)]?.unreadCount || 0,
    }),
    [threads, minimized, supportOpen, selectedOrderId, hydrateThread, sendMessage, clearUnread, closeBubble, reopenBubble, endConversation, openSupport, closeSupport]
  );

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
}

export function useChat() {
  const ctx = useContext(ChatContext);
  if (!ctx) throw new Error("useChat must be used within ChatProvider");
  return ctx;
}
