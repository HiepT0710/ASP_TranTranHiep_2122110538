import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { createOrderChatConnection, getOrderChatMessages, sendOrderChatMessage } from "../services/apiService";
import { useAuth } from "./AuthContext";
import { useToast } from "./ToastContext";

const ChatContext = createContext(null);
const STORAGE_KEY = "chat-ui-state-v2";

function loadSavedState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return { threads: {}, minimized: {}, selectedOrderId: null, selectedTarget: "seller" };
    const parsed = JSON.parse(raw);
    const defaultTarget = normalizeTarget(parsed?.selectedTarget || "seller");
    const normalizedThreads = {};
    const normalizedMinimized = {};

    for (const [threadKey, threadValue] of Object.entries(parsed?.threads || {})) {
      const parsedKey = parseThreadKey(threadKey);
      const orderId = String(threadValue?.orderId ?? parsedKey.orderId ?? "").trim();
      if (!orderId) continue;
      const target = normalizeTarget(threadValue?.target ?? parsedKey.target ?? defaultTarget);
      const nextKey = makeThreadKey(orderId, target);
      normalizedThreads[nextKey] = {
        ...buildThreadDefaults(target),
        ...(threadValue || {}),
        orderId,
        target,
        messages: Array.isArray(threadValue?.messages) ? threadValue.messages : [],
      };
    }

    for (const [threadKey, minimizedValue] of Object.entries(parsed?.minimized || {})) {
      const parsedKey = parseThreadKey(threadKey);
      const orderId = String(parsedKey.orderId || threadKey || "").trim();
      if (!orderId) continue;
      const target = normalizeTarget(parsedKey.target || defaultTarget);
      normalizedMinimized[makeThreadKey(orderId, target)] = Boolean(minimizedValue);
    }

    return {
      threads: normalizedThreads,
      minimized: normalizedMinimized,
      selectedOrderId: parsed.selectedOrderId ?? null,
      selectedTarget: defaultTarget,
    };
  } catch {
    return { threads: {}, minimized: {}, selectedOrderId: null, selectedTarget: "seller" };
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
    target: normalizeTarget(message.target ?? message.Target ?? "seller"),
    createdAt,
    mine: String(message.userId ?? message.UserId ?? "") === String(currentUserId),
    ended: /\/(end|kết thúc)\b/i.test(text),
  };
}

function normalizeTarget(target) {
  return String(target || "seller").toLowerCase() === "admin" ? "admin" : "seller";
}

function getTargetByRole(role, selectedTarget = "seller") {
  if (role === "Admin") return "admin";
  if (role === "Seller") return "seller";
  return normalizeTarget(selectedTarget);
}

function makeThreadKey(orderId, target) {
  return `${String(orderId)}::${normalizeTarget(target)}`;
}

function parseThreadKey(threadKey) {
  const [orderId, target] = String(threadKey || "").split("::");
  return {
    orderId: orderId || "",
    target: normalizeTarget(target),
  };
}

function buildThreadDefaults(target = "seller") {
  return {
    orderId: null,
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
        const messageTarget = getTargetByRole(user?.role, normalized.target);
        const key = makeThreadKey(normalized.orderId, messageTarget);
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
          const thread = current[key] || buildThreadDefaults(messageTarget);
          const partnerName = normalized.mine ? thread.partnerName : (normalized.username || thread.partnerName || "Hỗ trợ");
          const messages = mergeMessages(thread.messages, [normalized]);
          return {
            ...current,
            [key]: {
              ...thread,
              orderId: String(normalized.orderId),
              target: messageTarget,
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
          setSelectedOrderId(String(normalized.orderId));
          setSelectedTarget(messageTarget);
          setSupportOpen(true);
          void hydrateThread(normalized.orderId, messageTarget);
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
  }, [isAuthenticated, user?.id, user?.role, pushToast]);

  const upsertThread = useCallback((orderId, target, updater) => {
    const key = makeThreadKey(orderId, target);
    setThreads((current) => {
      const existing = restoreThread(current[key]);
      const next = updater(existing);
      return { ...current, [key]: next };
    });
  }, []);

  const hydrateThread = useCallback(async (orderId, target = selectedTarget) => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    const key = makeThreadKey(orderId, resolvedTarget);
    let alreadyLoaded = false;
    setThreads((current) => {
      const existing = current[key];
      alreadyLoaded = !!existing?.loaded;
      return {
        ...current,
        [key]: existing || { ...buildThreadDefaults(resolvedTarget), orderId: String(orderId), target: resolvedTarget },
      };
    });
    if (alreadyLoaded) return;
    try {
      const history = await getOrderChatMessages(orderId, { page: 1, pageSize: 100, target: resolvedTarget });
      const messages = (history.items || [])
        .map((item) => normalizeChatMessage(item, orderId, user?.id))
        .filter(Boolean);
      setThreads((current) => {
        const existing = restoreThread(current[key]);
        return {
          ...current,
          [key]: {
            ...existing,
            orderId: String(orderId),
            target: resolvedTarget,
            messages: mergeMessages(existing.messages, messages),
            loaded: true,
            lastActivityAt: messages.length ? Date.now() : existing.lastActivityAt,
          },
        };
      });
    } catch {
      // ignore
    }
  }, [selectedTarget, user?.id, user?.role]);

  const sendMessage = useCallback(async (orderId, message, target = selectedTarget) => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    const key = makeThreadKey(orderId, resolvedTarget);
    const optimistic = normalizeChatMessage({ orderId, userId: user?.id, username: user?.fullName || user?.username || "Bạn", message, createdAt: new Date().toISOString() }, orderId, user?.id);
    if (optimistic) {
      setThreads((current) => {
        const thread = restoreThread(current[key]);
        const messages = mergeMessages(thread.messages, [{ ...optimistic, target: resolvedTarget }]);
        return {
          ...current,
          [key]: {
            ...thread,
            orderId: String(orderId),
            target: resolvedTarget,
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
    await sendOrderChatMessage(connectionRef.current, orderId, message, resolvedTarget);
  }, [selectedTarget, user?.id, user?.fullName, user?.role, user?.username]);

  const clearUnread = useCallback((orderId, target = selectedTarget) => {
    upsertThread(orderId, getTargetByRole(user?.role, target), (thread) => ({ ...thread, unreadCount: 0 }));
  }, [selectedTarget, upsertThread, user?.role]);

  const closeBubble = useCallback((orderId, target = selectedTarget) => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    const key = makeThreadKey(orderId, resolvedTarget);
    setMinimized((current) => ({ ...current, [key]: true }));
    upsertThread(orderId, resolvedTarget, (thread) => ({ ...thread, open: false }));
  }, [selectedTarget, upsertThread, user?.role]);

  const reopenBubble = useCallback((orderId, target = selectedTarget) => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    const key = makeThreadKey(orderId, resolvedTarget);
    setMinimized((current) => ({ ...current, [key]: false }));
    upsertThread(orderId, resolvedTarget, (thread) => ({ ...thread, open: true }));
  }, [selectedTarget, upsertThread, user?.role]);

  const endConversation = useCallback((orderId, target = selectedTarget) => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    const key = makeThreadKey(orderId, resolvedTarget);
    setMinimized((current) => ({ ...current, [key]: true }));
    upsertThread(orderId, resolvedTarget, (thread) => ({ ...thread, open: false, ended: true, agentStatus: "kết thúc" }));
  }, [selectedTarget, upsertThread, user?.role]);

  const openSupport = useCallback((orderId = null, target = "seller") => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    if (orderId == null) {
      const latestThread = Object.entries(threads)
        .map(([threadKey, thread]) => ({ threadKey, thread }))
        .filter((item) => !item.thread?.ended)
        .sort((a, b) => (b.thread?.lastActivityAt || 0) - (a.thread?.lastActivityAt || 0))[0];
      if (latestThread?.thread?.orderId) {
        setSelectedOrderId(String(latestThread.thread.orderId));
        setSelectedTarget(normalizeTarget(latestThread.thread.target || resolvedTarget));
        setSupportOpen(true);
        setMinimized((current) => ({ ...current, [latestThread.threadKey]: false }));
        setThreads((current) => ({
          ...current,
          [latestThread.threadKey]: {
            ...restoreThread(current[latestThread.threadKey]),
            open: true,
          },
        }));
        return;
      }
    }

    setSelectedOrderId(orderId != null ? String(orderId) : null);
    setSelectedTarget(resolvedTarget);
    setSupportOpen(true);
    if (orderId != null) hydrateThread(orderId, resolvedTarget).catch(() => {});
  }, [hydrateThread, threads, user?.role]);
  const closeSupport = useCallback(() => setSupportOpen(false), []);

  const getThreadByOrder = useCallback((orderId, target = selectedTarget) => {
    const resolvedTarget = getTargetByRole(user?.role, target);
    return threads[makeThreadKey(orderId, resolvedTarget)] || null;
  }, [selectedTarget, threads, user?.role]);

  const value = useMemo(
    () => ({
      threads,
      minimized,
      supportOpen,
      selectedOrderId,
      selectedTarget,
      setSelectedTarget,
      hydrateThread,
      sendMessage,
      clearUnread,
      closeBubble,
      reopenBubble,
      endConversation,
      openSupport,
      closeSupport,
      getThreadByOrder,
      getUnreadCount: (orderId, target = selectedTarget) => {
        const resolvedTarget = getTargetByRole(user?.role, target);
        return threads[makeThreadKey(orderId, resolvedTarget)]?.unreadCount || 0;
      },
    }),
    [threads, minimized, supportOpen, selectedOrderId, selectedTarget, hydrateThread, sendMessage, clearUnread, closeBubble, reopenBubble, endConversation, openSupport, closeSupport, getThreadByOrder, user?.role]
  );

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>;
}

export function useChat() {
  const ctx = useContext(ChatContext);
  if (!ctx) throw new Error("useChat must be used within ChatProvider");
  return ctx;
}
