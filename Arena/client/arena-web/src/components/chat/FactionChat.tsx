import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Send, MessageCircle, WifiOff, AlertCircle, LogIn } from 'lucide-react';
import { useFactionChat, ConnectionStatus } from '../../hooks/useFactionChat';
import { useAuth } from '../../context/AuthContext';
import { ChatMessage } from '../../types';

interface FactionChatProps {
  teamId: number | null;
}

/* ─── Status Indicator Dot ─── */
const StatusDot: React.FC<{ status: ConnectionStatus }> = ({ status }) => {
  const config: Record<ConnectionStatus, { color: string; label: string }> = {
    connected: { color: 'bg-emerald-400', label: 'Connected' },
    connecting: { color: 'bg-yellow-400 animate-pulse', label: 'Connecting...' },
    disconnected: { color: 'bg-zinc-500', label: 'Disconnected' },
    error: { color: 'bg-red-500', label: 'Error' },
  };
  const { color, label } = config[status];
  return (
    <div className="flex items-center gap-1.5">
      <span className={`inline-block w-2 h-2 rounded-full ${color}`} />
      <span className="text-xs text-arena-textMuted">{label}</span>
    </div>
  );
};

/* ─── Chat Header ─── */
const ChatHeader: React.FC<{ status: ConnectionStatus }> = ({ status }) => (
  <div className="flex items-center justify-between px-4 py-3 border-b border-arena-border bg-arena-surface/80 backdrop-blur-sm">
    <div className="flex items-center gap-2">
      <MessageCircle size={16} className="text-arena-cyan" />
      <span className="text-sm font-bold uppercase tracking-wider text-arena-text">
        Faction Chat
      </span>
    </div>
    <StatusDot status={status} />
  </div>
);

/* ─── Single Chat Bubble ─── */
const ChatBubble: React.FC<{ msg: ChatMessage; isOwn: boolean }> = ({ msg, isOwn }) => {
  const time = new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  const initial = msg.username.charAt(0).toUpperCase();

  return (
    <div className={`flex gap-2.5 px-4 py-1.5 group hover:bg-arena-surfaceHover/40 transition-colors ${isOwn ? '' : ''}`}>
      {/* Avatar circle with team color */}
      <div
        className="w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5 text-xs font-bold text-white"
        style={{ backgroundColor: msg.teamColor || '#71717A' }}
      >
        {initial}
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-baseline gap-2">
          <span
            className="text-sm font-semibold truncate"
            style={{ color: msg.teamColor || '#FFFFFF' }}
          >
            {msg.username}
          </span>
          <span className="text-[10px] text-arena-textMuted opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
            {time}
          </span>
        </div>
        <p className="text-sm text-arena-text break-words leading-relaxed">
          {msg.content}
        </p>
      </div>
    </div>
  );
};

/* ─── System Error Message ─── */
const SystemMessage: React.FC<{ text: string }> = ({ text }) => (
  <div className="flex items-center justify-center gap-2 py-2 px-4">
    <div className="flex items-center gap-1.5 bg-red-500/10 border border-red-500/20 rounded-full px-3 py-1">
      <AlertCircle size={12} className="text-red-400" />
      <span className="text-xs text-red-400">{text}</span>
    </div>
  </div>
);

/* ─── Message List ─── */
const MessageList: React.FC<{ messages: ChatMessage[]; currentUserId: number | undefined; error: string | null }> = ({
  messages,
  currentUserId,
  error,
}) => {
  const bottomRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const shouldAutoScrollRef = useRef(true);

  const handleScroll = useCallback(() => {
    const el = containerRef.current;
    if (!el) return;
    // Auto-scroll if user is near the bottom (within 100px)
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    shouldAutoScrollRef.current = distanceFromBottom < 100;
  }, []);

  useEffect(() => {
    if (shouldAutoScrollRef.current) {
      bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, error]);

  if (messages.length === 0 && !error) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center text-center px-6 py-8">
        <MessageCircle size={32} className="text-arena-border mb-3" />
        <p className="text-sm text-arena-textMuted">No messages yet.</p>
        <p className="text-xs text-arena-textMuted mt-1">Be the first to chat!</p>
      </div>
    );
  }

  return (
    <div
      ref={containerRef}
      onScroll={handleScroll}
      className="flex-1 overflow-y-auto py-2 space-y-0.5 scrollbar-thin"
    >
      {messages.map((msg) => (
        <ChatBubble
          key={msg.messageId}
          msg={msg}
          isOwn={msg.userId === currentUserId}
        />
      ))}
      {error && <SystemMessage text={error} />}
      <div ref={bottomRef} />
    </div>
  );
};

/* ─── Chat Input Bar ─── */
const ChatInput: React.FC<{
  onSend: (text: string) => void;
  disabled: boolean;
  connectionStatus: ConnectionStatus;
}> = ({ onSend, disabled, connectionStatus }) => {
  const [text, setText] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  const handleSend = () => {
    const trimmed = text.trim();
    if (!trimmed) return;
    onSend(trimmed);
    setText('');
    inputRef.current?.focus();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const charCount = text.length;
  const isOverLimit = charCount > 450;
  const isAtLimit = charCount >= 500;
  const isDisabled = disabled || connectionStatus !== 'connected';

  return (
    <div className="border-t border-arena-border bg-arena-surface/80 backdrop-blur-sm px-3 py-2.5">
      <div className="flex items-center gap-2">
        <input
          ref={inputRef}
          type="text"
          value={text}
          onChange={(e) => {
            if (e.target.value.length <= 500) setText(e.target.value);
          }}
          onKeyDown={handleKeyDown}
          disabled={isDisabled}
          placeholder={
            isDisabled
              ? connectionStatus === 'connecting' ? 'Connecting...' : 'Chat unavailable'
              : 'Send a message...'
          }
          className="flex-1 bg-arena-bg border border-arena-border rounded-lg px-3 py-2 text-sm text-arena-text placeholder-arena-textMuted focus:outline-none focus:border-arena-borderFocus transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          maxLength={500}
          id="chat-message-input"
        />
        <button
          onClick={handleSend}
          disabled={isDisabled || !text.trim()}
          className="p-2 rounded-lg bg-arena-cyan hover:bg-arena-cyanHover text-white transition-colors disabled:opacity-30 disabled:cursor-not-allowed flex-shrink-0"
          id="chat-send-button"
        >
          <Send size={16} />
        </button>
      </div>

      {/* Character counter — only visible when typing */}
      {charCount > 0 && (
        <div className="flex justify-end mt-1 pr-1">
          <span
            className={`text-[10px] transition-colors ${
              isAtLimit ? 'text-arena-crimson font-bold' : isOverLimit ? 'text-arena-crimson' : 'text-arena-textMuted'
            }`}
          >
            {charCount}/500
          </span>
        </div>
      )}
    </div>
  );
};

/* ─── Login Prompt ─── */
const LoginPrompt: React.FC = () => (
  <div className="flex-1 flex flex-col items-center justify-center text-center px-6 py-8">
    <LogIn size={28} className="text-arena-textMuted mb-3" />
    <p className="text-sm text-arena-textMuted font-semibold">Log in to join the chat</p>
    <a
      href="/login"
      className="mt-2 text-xs text-arena-cyan hover:underline font-medium"
    >
      Go to Login →
    </a>
  </div>
);

/* ─── Team Select Prompt ─── */
const TeamSelectPrompt: React.FC = () => (
  <div className="flex-1 flex flex-col items-center justify-center text-center px-6 py-8">
    <MessageCircle size={28} className="text-arena-textMuted mb-3" />
    <p className="text-sm text-arena-textMuted font-semibold">Select a team to join faction chat</p>
    <p className="text-xs text-arena-textMuted mt-1">Choose your side above to start chatting</p>
  </div>
);

/* ─── Disconnected Overlay ─── */
const DisconnectedOverlay: React.FC<{ status: ConnectionStatus }> = ({ status }) => {
  if (status === 'connected' || status === 'connecting') return null;
  return (
    <div className="absolute inset-0 bg-arena-bg/60 backdrop-blur-sm flex flex-col items-center justify-center z-10 rounded-b-[14px]">
      <WifiOff size={24} className="text-arena-textMuted mb-2" />
      <p className="text-sm text-arena-textMuted font-semibold">Connection lost</p>
      <p className="text-xs text-arena-textMuted mt-1">Attempting to reconnect…</p>
    </div>
  );
};

/* ═══════════════════════════════════════════════
   Main FactionChat Component
   ═══════════════════════════════════════════════ */
export const FactionChat: React.FC<FactionChatProps> = ({ teamId }) => {
  const { user } = useAuth();
  const { messages, sendMessage, connectionStatus, error } = useFactionChat(
    user && teamId ? teamId : null
  );

  return (
    <div className="w-full h-full min-h-[400px] bg-arena-surface border border-arena-border rounded-[14px] flex flex-col overflow-hidden relative" id="faction-chat-panel">
      <ChatHeader status={user && teamId ? connectionStatus : 'disconnected'} />

      {!user ? (
        <LoginPrompt />
      ) : !teamId ? (
        <TeamSelectPrompt />
      ) : (
        <>
          {connectionStatus === 'disconnected' && messages.length > 0 && (
            <DisconnectedOverlay status={connectionStatus} />
          )}
          <MessageList
            messages={messages}
            currentUserId={user.userId}
            error={error}
          />
          <ChatInput
            onSend={sendMessage}
            disabled={!user || !teamId}
            connectionStatus={connectionStatus}
          />
        </>
      )}
    </div>
  );
};

export default FactionChat;
