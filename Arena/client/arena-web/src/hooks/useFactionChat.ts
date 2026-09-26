import { useState, useEffect, useRef, useCallback } from 'react';
import { ChatMessage, ChatFrame } from '../types';

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'error';

const MAX_MESSAGES = 200;
const MAX_RECONNECT_DELAY = 16000;
const INITIAL_RECONNECT_DELAY = 1000;

/**
 * Custom hook for managing a WebSocket connection to the Arena faction chat.
 * Handles connection lifecycle, reconnection with exponential backoff,
 * message history hydration, and live message streaming.
 */
export function useFactionChat(teamId: number | null) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');
  const [error, setError] = useState<string | null>(null);

  const wsRef = useRef<WebSocket | null>(null);
  const reconnectDelayRef = useRef(INITIAL_RECONNECT_DELAY);
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mountedRef = useRef(true);

  const clearReconnectTimer = useCallback(() => {
    if (reconnectTimerRef.current) {
      clearTimeout(reconnectTimerRef.current);
      reconnectTimerRef.current = null;
    }
  }, []);

  const connect = useCallback(() => {
    if (!teamId || teamId <= 0) return;

    const token = localStorage.getItem('arena_access_token');
    if (!token) {
      setConnectionStatus('disconnected');
      return;
    }

    // Close existing connection before opening a new one
    if (wsRef.current) {
      wsRef.current.onclose = null; // Prevent reconnect on intentional close
      wsRef.current.close();
      wsRef.current = null;
    }

    setConnectionStatus('connecting');
    setError(null);

    // Resolve WebSocket URL
    const wsBase = import.meta.env.VITE_CHAT_WS_URL
      || `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}`;
    const wsUrl = `${wsBase}/ws/chat?teamId=${teamId}&token=${token}`;

    const ws = new WebSocket(wsUrl);
    wsRef.current = ws;

    ws.onopen = () => {
      if (!mountedRef.current) return;
      setConnectionStatus('connected');
      reconnectDelayRef.current = INITIAL_RECONNECT_DELAY; // Reset backoff on success
    };

    ws.onmessage = (event) => {
      if (!mountedRef.current) return;

      try {
        const frame: ChatFrame = JSON.parse(event.data);

        switch (frame.type) {
          case 'history':
            setMessages(frame.messages.slice(-MAX_MESSAGES));
            break;

          case 'message':
            setMessages(prev => {
              const next = [...prev, frame as ChatMessage];
              return next.length > MAX_MESSAGES ? next.slice(-MAX_MESSAGES) : next;
            });
            break;

          case 'error':
            setError(frame.message);
            // Auto-clear error after 5 seconds
            setTimeout(() => {
              if (mountedRef.current) setError(null);
            }, 5000);
            break;
        }
      } catch {
        console.error('[FactionChat] Failed to parse WebSocket frame');
      }
    };

    ws.onerror = () => {
      if (!mountedRef.current) return;
      setConnectionStatus('error');
    };

    ws.onclose = (event) => {
      if (!mountedRef.current) return;
      wsRef.current = null;
      setConnectionStatus('disconnected');

      // Don't reconnect on normal closure (code 1000) or auth failure (code 1008)
      if (event.code === 1000 || event.code === 1008) return;

      // Exponential backoff reconnect
      const delay = reconnectDelayRef.current;
      reconnectDelayRef.current = Math.min(delay * 2, MAX_RECONNECT_DELAY);
      
      reconnectTimerRef.current = setTimeout(() => {
        if (mountedRef.current) connect();
      }, delay);
    };
  }, [teamId]);

  // Connect/reconnect when teamId changes
  useEffect(() => {
    mountedRef.current = true;
    setMessages([]);
    setError(null);
    clearReconnectTimer();

    if (teamId && teamId > 0) {
      connect();
    } else {
      setConnectionStatus('disconnected');
    }

    return () => {
      mountedRef.current = false;
      clearReconnectTimer();
      if (wsRef.current) {
        wsRef.current.onclose = null;
        wsRef.current.close();
        wsRef.current = null;
      }
    };
  }, [teamId, connect, clearReconnectTimer]);

  const sendMessage = useCallback((text: string) => {
    if (!wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) return;
    
    const trimmed = text.trim();
    if (!trimmed || trimmed.length > 500) return;

    // Send plain text (not JSON) — matches the backend protocol
    wsRef.current.send(trimmed);
  }, []);

  return { messages, sendMessage, connectionStatus, error };
}
