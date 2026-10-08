import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Send, MessageCircle, WifiOff, AlertCircle, LogIn } from 'lucide-react';
import { useFactionChat, ConnectionStatus } from '../../hooks/useFactionChat';
import { useAuth } from '../../context/AuthContext';
import { ChatMessage } from '../../types';

interface FactionChatProps {
  matchId?: number | string | null;
  teamAId?: number | null;
  teamBId?: number | null;
  selectedTeamId?: number | null;
  teamsMap?: Record<number, { name: string; color: string }>;
  onSelectTeam?: (teamId: number) => void;
  activeChannel?: string | null;
  /** Legacy single-team prop fallback */
  teamId?: number | null;
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
const ChatHeader: React.FC<{
  status: ConnectionStatus;
  activeTeam?: { name: string; color: string } | null;
}> = ({ status, activeTeam }) => (
  <div className="flex items-center justify-between px-4 py-3 border-b border-arena-border bg-arena-surface/90 backdrop-blur-sm">
    <div className="flex items-center gap-2 min-w-0">
      <MessageCircle size={16} className="text-arena-cyan shrink-0" />
      <span className="text-sm font-bold uppercase tracking-wider text-arena-text truncate">
        Faction Chat
      </span>
      {activeTeam && (
        <span
          className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase tracking-wider shrink-0"
          style={{
            color: activeTeam.color,
            backgroundColor: `${activeTeam.color}15`,
            border: `1px solid ${activeTeam.color}40`,
          }}
        >
          {activeTeam.name}
        </span>
      )}
    </div>
    <StatusDot status={status} />
  </div>
);

/* ─── Single Chat Bubble with Inline Team Color Username and Message ─── */
const ChatBubble: React.FC<{
  msg: ChatMessage;
  isOwn: boolean;
  fallbackColor?: string;
}> = ({ msg, fallbackColor = '#00B8FC' }) => {
  const time = new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  const initial = msg.username.charAt(0).toUpperCase();
  const teamColor = msg.teamColor && msg.teamColor !== '#FFFFFF' ? msg.teamColor : fallbackColor;

  return (
    <div className="flex items-start gap-2.5 px-3.5 py-1.5 group hover:bg-arena-surfaceHover/30 transition-colors text-xs sm:text-sm">
      {/* Avatar circle with team color */}
      <div
        className="w-5 h-5 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5 text-[10px] font-bold text-white shadow-sm"
        style={{ backgroundColor: teamColor }}
        title={msg.username}
      >
        {initial}
      </div>

      {/* Inline username and message on the same line */}
      <div className="flex-1 min-w-0 leading-snug">
        <span
          className="font-bold mr-1.5 tracking-wide hover:underline cursor-pointer select-text"
          style={{ color: teamColor }}
        >
          {msg.username}:
        </span>
        <span className="text-arena-text font-normal break-words select-text">
          {msg.content}
        </span>
      </div>

      {/* Timestamp on hover */}
      <span className="text-[10px] text-arena-textMuted opacity-0 group-hover:opacity-100 transition-opacity ml-1 flex-shrink-0 select-none">
        {time}
      </span>
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
const MessageList: React.FC<{
  messages: ChatMessage[];
  currentUserId: number | undefined;
  error: string | null;
  teamsMap?: Record<number, { name: string; color: string }>;
}> = ({ messages, currentUserId, error, teamsMap }) => {
  const bottomRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const shouldAutoScrollRef = useRef(true);

  const handleScroll = useCallback(() => {
    const el = containerRef.current;
    if (!el) return;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    shouldAutoScrollRef.current = distanceFromBottom < 100;
  }, []);

  useEffect(() => {
    if (shouldAutoScrollRef.current && containerRef.current) {
      containerRef.current.scrollTo({
        top: containerRef.current.scrollHeight,
        behavior: 'smooth',
      });
    }
  }, [messages, error]);

  if (messages.length === 0 && !error) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center text-center px-6 py-8">
        <MessageCircle size={32} className="text-arena-border mb-3 opacity-40" />
        <p className="text-sm font-semibold text-arena-textMuted">No messages yet.</p>
        <p className="text-xs text-arena-textMuted mt-1">Be the first to rally your faction!</p>
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
          fallbackColor={teamsMap?.[msg.teamId]?.color}
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
  activeTeam?: { name: string; color: string } | null;
  teamA?: { teamId: number; name: string; color: string } | null;
  teamB?: { teamId: number; name: string; color: string } | null;
  onSelectTeam?: (teamId: number) => void;
}> = ({ onSend, disabled, connectionStatus, activeTeam, teamA, teamB, onSelectTeam }) => {
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

  // If user is connected but hasn't selected a faction yet: show quick selection buttons
  if (!activeTeam && (teamA || teamB)) {
    return (
      <div className="border-t border-arena-border bg-arena-surface/90 backdrop-blur-sm p-3">
        <p className="text-[11px] text-arena-textMuted uppercase font-bold tracking-wider text-center mb-2">
          Choose a faction to send messages:
        </p>
        <div className="flex gap-2">
          {teamA && (
            <button
              onClick={() => onSelectTeam?.(teamA.teamId)}
              className="flex-1 py-2 px-3 rounded-lg text-xs font-bold uppercase tracking-wider transition-all duration-200 border"
              style={{
                borderColor: `${teamA.color}50`,
                backgroundColor: `${teamA.color}15`,
                color: teamA.color,
              }}
            >
              Support {teamA.name}
            </button>
          )}
          {teamB && (
            <button
              onClick={() => onSelectTeam?.(teamB.teamId)}
              className="flex-1 py-2 px-3 rounded-lg text-xs font-bold uppercase tracking-wider transition-all duration-200 border"
              style={{
                borderColor: `${teamB.color}50`,
                backgroundColor: `${teamB.color}15`,
                color: teamB.color,
              }}
            >
              Support {teamB.name}
            </button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="border-t border-arena-border bg-arena-surface/90 backdrop-blur-sm px-3 py-2.5">
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
              ? connectionStatus === 'connecting'
                ? 'Connecting to chat...'
                : 'Chat unavailable'
              : activeTeam
              ? `Chat as [${activeTeam.name}]...`
              : 'Send a message...'
          }
          className="flex-1 bg-arena-bg border border-arena-border rounded-lg px-3 py-2 text-sm text-arena-text placeholder-arena-textMuted focus:outline-none focus:border-arena-cyan transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          maxLength={500}
          id="chat-message-input"
        />
        <button
          onClick={handleSend}
          disabled={isDisabled || !text.trim()}
          className="p-2 rounded-lg text-white transition-colors disabled:opacity-30 disabled:cursor-not-allowed flex-shrink-0"
          style={{
            backgroundColor: activeTeam?.color || '#00B8FC',
          }}
          id="chat-send-button"
          aria-label="Send message"
        >
          <Send size={16} />
        </button>
      </div>

      {charCount > 0 && (
        <div className="flex justify-end mt-1 pr-1">
          <span
            className={`text-[10px] transition-colors ${
              isAtLimit
                ? 'text-arena-crimson font-bold'
                : isOverLimit
                ? 'text-arena-crimson'
                : 'text-arena-textMuted'
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
  <div className="border-t border-arena-border bg-arena-surface/90 backdrop-blur-sm p-4 text-center">
    <div className="flex items-center justify-center gap-2 text-arena-textMuted text-xs font-semibold mb-2">
      <LogIn size={16} />
      <span>Sign in to participate in the battle chat</span>
    </div>
    <a
      href="/login"
      className="inline-block py-1.5 px-4 rounded-lg bg-arena-cyan/15 border border-arena-cyan/40 text-arena-cyan text-xs font-bold uppercase tracking-wider hover:bg-arena-cyan/25 transition-colors"
    >
      Sign In
    </a>
  </div>
);

/* ─── Disconnected Overlay ─── */
const DisconnectedOverlay: React.FC<{ status: ConnectionStatus }> = ({ status }) => {
  if (status === 'connected' || status === 'connecting') return null;
  return (
    <div className="absolute inset-0 bg-arena-bg/60 backdrop-blur-sm flex flex-col items-center justify-center z-10 rounded-b-[14px]">
      <WifiOff size={24} className="text-arena-textMuted mb-2" />
      <p className="text-sm text-arena-textMuted font-semibold">Chat connection lost</p>
      <p className="text-xs text-arena-textMuted mt-1">Reconnecting automatically…</p>
    </div>
  );
};

/* ═══════════════════════════════════════════════
   Main FactionChat Component
   ═══════════════════════════════════════════════ */
export const FactionChat: React.FC<FactionChatProps> = ({
  matchId,
  teamAId,
  teamBId,
  selectedTeamId,
  teamsMap,
  onSelectTeam,
  activeChannel,
  teamId, // Legacy fallback
}) => {
  const { user } = useAuth();

  // Resolve team IDs
  const resolvedTeamAId = teamAId ?? teamId ?? null;
  const resolvedTeamBId = teamBId ?? null;
  const activeSendTeamId = selectedTeamId ?? null;

  const { messages, sendMessage, connectionStatus, error } = useFactionChat({
    matchId,
    teamAId: resolvedTeamAId,
    teamBId: resolvedTeamBId,
    activeSendTeamId,
    teamsMap,
    activeChannel,
  });

  const activeTeam =
    activeSendTeamId && teamsMap?.[activeSendTeamId]
      ? teamsMap[activeSendTeamId]
      : activeSendTeamId
      ? { name: `Team ${activeSendTeamId}`, color: '#00B8FC' }
      : null;

  const teamAInfo =
    resolvedTeamAId && teamsMap?.[resolvedTeamAId]
      ? { teamId: resolvedTeamAId, ...teamsMap[resolvedTeamAId] }
      : null;

  const teamBInfo =
    resolvedTeamBId && teamsMap?.[resolvedTeamBId]
      ? { teamId: resolvedTeamBId, ...teamsMap[resolvedTeamBId] }
      : null;

  return (
    <div
      className="w-full h-full min-h-0 bg-arena-surface border border-arena-border rounded-[14px] flex flex-col overflow-hidden relative"
      id="faction-chat-panel"
    >
      <ChatHeader
        status={user ? connectionStatus : 'disconnected'}
        activeTeam={activeTeam}
      />

      {connectionStatus === 'disconnected' && messages.length > 0 && user && (
        <DisconnectedOverlay status={connectionStatus} />
      )}

      <MessageList
        messages={messages}
        currentUserId={user?.userId}
        error={error}
        teamsMap={teamsMap}
      />

      {!user ? (
        <LoginPrompt />
      ) : (
        <ChatInput
          onSend={sendMessage}
          disabled={!user}
          connectionStatus={connectionStatus}
          activeTeam={activeTeam}
          teamA={teamAInfo}
          teamB={teamBInfo}
          onSelectTeam={onSelectTeam}
        />
      )}
    </div>
  );
};

export default FactionChat;
