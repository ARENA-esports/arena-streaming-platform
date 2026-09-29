import { useState, useEffect, useRef, useCallback } from 'react';
import { ChatMessage, ChatFrame } from '../types';

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'error';

const MAX_MESSAGES = 300;
const MAX_RECONNECT_DELAY = 16000;
const INITIAL_RECONNECT_DELAY = 1000;

export interface UseFactionChatOptions {
  matchId?: number | string | null;
  teamAId?: number | null;
  teamBId?: number | null;
  activeSendTeamId?: number | null;
  teamsMap?: Record<number, { name: string; color: string }>;
  activeChannel?: string | null;
}

const getStoredStreamMessages = (key: string): ChatMessage[] => {
  try {
    const raw = localStorage.getItem(`arena_stream_chat_${key}`);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
};

const saveStoredStreamMessages = (key: string, msgs: ChatMessage[]) => {
  try {
    localStorage.setItem(`arena_stream_chat_${key}`, JSON.stringify(msgs.slice(-MAX_MESSAGES)));
  } catch {
    // ignore quota error
  }
};

/**
 * Custom hook for managing a unified dual-faction WebSocket connection to Arena chat.
 * Connects to both teams in a match, merges incoming history and live messages into a
 * single timeline, sorts them chronologically, and cleans up / resets when switching streams.
 */
export function useFactionChat(
  teamIdOrOptions: number | null | UseFactionChatOptions,
  legacyActiveSendTeamId?: number | null
) {
  // Support both object options and legacy single-teamId signature
  const options: UseFactionChatOptions =
    typeof teamIdOrOptions === 'object' && teamIdOrOptions !== null
      ? teamIdOrOptions
      : {
          teamAId: teamIdOrOptions as number | null,
          teamBId: null,
          activeSendTeamId: legacyActiveSendTeamId ?? (teamIdOrOptions as number | null),
        };

  const { matchId, teamAId, teamBId, activeSendTeamId, teamsMap, activeChannel } = options;

  const normalizedChannel = (activeChannel || '').trim().toLowerCase();
  const streamKey = `m${matchId ?? '0'}_${normalizedChannel || 'default'}`;

  const [messages, setMessages] = useState<ChatMessage[]>(() => getStoredStreamMessages(streamKey));
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');
  const [error, setError] = useState<string | null>(null);

  const currentStreamKeyRef = useRef(streamKey);
  currentStreamKeyRef.current = streamKey;

  const wsMapRef = useRef<Map<number, WebSocket>>(new Map());
  const reconnectDelayRef = useRef(INITIAL_RECONNECT_DELAY);
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mountedRef = useRef(true);

  // When stream changes, wipe or restore the chat history for this specific stream
  useEffect(() => {
    const cached = getStoredStreamMessages(streamKey);
    setMessages(cached);
  }, [streamKey]);

  // Helper to deduplicate messages by messageId and sort chronologically ascending
  const mergeMessages = useCallback(
    (newMsgs: ChatMessage[], targetKey?: string) => {
      const key = targetKey || currentStreamKeyRef.current;
      setMessages((prev) => {
        const map = new Map<number, ChatMessage>();
        for (const m of prev) {
          map.set(m.messageId, m);
        }
        for (const m of newMsgs) {
          const enriched: ChatMessage = {
            ...m,
            teamName: m.teamName || teamsMap?.[m.teamId]?.name || `Team ${m.teamId}`,
            teamColor:
              m.teamColor && m.teamColor !== '#FFFFFF'
                ? m.teamColor
                : teamsMap?.[m.teamId]?.color || (m.teamId === teamAId ? '#EF4444' : '#00B8FC'),
          };
          map.set(m.messageId, enriched);
        }
        const sorted = Array.from(map.values()).sort(
          (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
        );
        const result = sorted.slice(-MAX_MESSAGES);
        saveStoredStreamMessages(key, result);
        return result;
      });
    },
    [teamsMap, teamAId]
  );

  const connectToTeam = useCallback(
    (tid: number, token: string) => {
      if (!tid || tid <= 0) return;

      const existing = wsMapRef.current.get(tid);
      if (
        existing &&
        (existing.readyState === WebSocket.OPEN || existing.readyState === WebSocket.CONNECTING)
      ) {
        return;
      }

      if (existing) {
        existing.onclose = null;
        existing.close();
        wsMapRef.current.delete(tid);
      }

      const wsBase =
        import.meta.env.VITE_CHAT_WS_URL ||
        `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}`;
      const wsUrl = `${wsBase}/ws/chat?teamId=${tid}&token=${token}`;

      const ws = new WebSocket(wsUrl);
      wsMapRef.current.set(tid, ws);

      ws.onopen = () => {
        if (!mountedRef.current) return;
        setConnectionStatus('connected');
        reconnectDelayRef.current = INITIAL_RECONNECT_DELAY;
      };

      ws.onmessage = (event) => {
        if (!mountedRef.current) return;
        try {
          const frame: ChatFrame = JSON.parse(event.data);
          switch (frame.type) {
            case 'history': {
              if (Array.isArray(frame.messages)) {
                const key = currentStreamKeyRef.current;
                const cached = getStoredStreamMessages(key);
                const isDefault = key.endsWith('_default') || key.endsWith('_arena_streams');
                // Only hydrate backend generic history on the default channel when there is no local cache yet
                if (isDefault && cached.length === 0) {
                  mergeMessages(frame.messages, key);
                }
              }
              break;
            }
            case 'message': {
              const key = currentStreamKeyRef.current;
              mergeMessages([frame as unknown as ChatMessage], key);
              break;
            }
            case 'error': {
              setError(frame.message ?? 'An unknown error occurred.');
              setTimeout(() => {
                if (mountedRef.current) setError(null);
              }, 5000);
              break;
            }
          }
        } catch (err) {
          console.error('[FactionChat] Failed to parse WebSocket frame', err);
        }
      };

      ws.onerror = () => {
        if (!mountedRef.current) return;
        const allFailed = Array.from(wsMapRef.current.values()).every(
          (s) => s.readyState === WebSocket.CLOSED || s.readyState === WebSocket.CLOSING
        );
        if (allFailed) setConnectionStatus('error');
      };

      ws.onclose = (event) => {
        if (!mountedRef.current) return;
        wsMapRef.current.delete(tid);

        const anyOpen = Array.from(wsMapRef.current.values()).some(
          (s) => s.readyState === WebSocket.OPEN
        );
        if (!anyOpen) {
          setConnectionStatus('disconnected');
        }

        if (event.code === 1000 || event.code === 1008) return;

        // Exponential backoff reconnect
        const delay = reconnectDelayRef.current;
        reconnectDelayRef.current = Math.min(delay * 2, MAX_RECONNECT_DELAY);
        if (!reconnectTimerRef.current) {
          reconnectTimerRef.current = setTimeout(() => {
            reconnectTimerRef.current = null;
            if (mountedRef.current) {
              const currentToken = localStorage.getItem('arena_access_token');
              if (currentToken) {
                if (teamAId) connectToTeam(teamAId, currentToken);
                if (teamBId) connectToTeam(teamBId, currentToken);
              }
            }
          }, delay);
        }
      };
    },
    [mergeMessages, teamAId, teamBId]
  );

  // Whenever matchId or team IDs change (switching streams):
  // Clean up old sockets, wipe/restore stream messages, connect to new stream's teams!
  useEffect(() => {
    mountedRef.current = true;
    setMessages(getStoredStreamMessages(streamKey));
    setError(null);

    if (reconnectTimerRef.current) {
      clearTimeout(reconnectTimerRef.current);
      reconnectTimerRef.current = null;
    }

    wsMapRef.current.forEach((ws) => {
      ws.onclose = null;
      ws.close();
    });
    wsMapRef.current.clear();

    const token = localStorage.getItem('arena_access_token');
    if (!token || (!teamAId && !teamBId)) {
      setConnectionStatus('disconnected');
      return;
    }

    setConnectionStatus('connecting');
    if (teamAId && teamAId > 0) connectToTeam(teamAId, token);
    if (teamBId && teamBId > 0 && teamBId !== teamAId) connectToTeam(teamBId, token);

    return () => {
      mountedRef.current = false;
      if (reconnectTimerRef.current) {
        clearTimeout(reconnectTimerRef.current);
        reconnectTimerRef.current = null;
      }
      wsMapRef.current.forEach((ws) => {
        ws.onclose = null;
        ws.close();
      });
      wsMapRef.current.clear();
    };
  }, [matchId, teamAId, teamBId, connectToTeam]);

  const sendMessage = useCallback(
    (text: string) => {
      const trimmed = text.trim();
      if (!trimmed || trimmed.length > 500) return;

      if (!activeSendTeamId) {
        setError('Please select a faction above to send a message.');
        setTimeout(() => {
          if (mountedRef.current) setError(null);
        }, 4000);
        return;
      }

      const ws = wsMapRef.current.get(activeSendTeamId);
      if (!ws || ws.readyState !== WebSocket.OPEN) {
        setError('Not connected to your faction channel. Reconnecting...');
        const token = localStorage.getItem('arena_access_token');
        if (token) connectToTeam(activeSendTeamId, token);
        return;
      }

      ws.send(trimmed);
    },
    [activeSendTeamId, connectToTeam]
  );

  return { messages, sendMessage, connectionStatus, error };
}
