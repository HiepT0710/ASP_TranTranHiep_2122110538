import { useMemo } from "react";
import { useChat } from "../context/ChatContext";
import ChatFloatingBubble from "./ChatFloatingBubble";

export default function ChatOrderStack() {
  const { threads } = useChat();
  const active = useMemo(() => Object.entries(threads).filter(([, thread]) => thread?.messages?.length && !thread.ended), [threads]);
  if (active.length === 0) return null;
  return <ChatFloatingBubble />;
}
