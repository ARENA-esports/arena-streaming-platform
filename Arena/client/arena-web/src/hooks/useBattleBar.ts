import { useState, useEffect, useRef, useCallback } from 'react';
import { apiClient } from '../api/client';
import { BattleBarEntry, BattleBarBroadcastPayload, LatestAttackEvent } from '../types';

export type BattleBarConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'error';

interface UseBattleBarOptions {
  matchId: number | string | null | undefined;
  teamAId?: number | null;
  teamBId?: number | null;
}

export interface UseBattleBarResult {
  bars: Record<number, number>; // teamId -> totalDamage
  teamADamage: number;
  teamBDamage: number;
  latestAttack: LatestAttackEvent | null;
  connectionStatus: BattleBarConnectionStatus;
  isLoading: boolean;
  refetch: () => Promise<void>;
}

const INITIAL_RECONNECT_DELAY = 1000;
const MAX_RECONNECT_DELAY = 16000;

export function useBattleBar(options: UseBattleBarOptions | number | string | null | undefined): UseBattleBarResult {
  const matchId = typeof options === 'object' && options !== null ? options.matchId : options;
  const teamAId = typeof options === 'object' && options !== null ? options.teamAId : null;
  const teamBId = typeof options === 'object' && options !== null ? options.teamBId : null;

  const numericMatchId = matchId ? Number(matchId) : null;

  const [bars, setBars] = useState<Record<number, number>>({});
  const [latestAttack, setLatestAttack] = useState<LatestAttackEvent | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<BattleBarConnectionStatus>('disconnected');
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const wsRef = useRef<WebSocket | null>(null);
  const reconnectDelayRef = useRef(INITIAL_RECONNECT_DELAY);
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mountedRef = useRef(true);

  // Fallback / initial HTTP fetch for immediate display
  const fetchBattleBars = useCallback(async () => {
    if (!numericMatchId || numericMatchId <= 0) return;
    try {
      const response = await apiClient.get<BattleBarEntry[]>(`/economy/battle-bar/${numericMatchId}`);
      if (mountedRef.current && Array.isArray(response.data)) {
        const mapped: Record<number, number> = {};
        for (const item of response.data) {
          mapped[item.teamId] = item.totalDamage;
        }
        setBars(prev => ({ ...prev, ...mapped }));
      }
    } catch (err) {
      console.warn('Initial battle-bar fetch note:', err);
    } finally {
      if (mountedRef.current) {
        setIsLoading(false);
      }
    }
  }, [numericMatchId]);

  // Connect to real-time WebSocket broadcast room
  const connectWebSocket = useCallback(() => {
    if (!numericMatchId || numericMatchId <= 0) {
      setConnectionStatus('disconnected');
      return;
    }

    if (
      wsRef.current &&
      (wsRef.current.readyState === WebSocket.OPEN || wsRef.current.readyState === WebSocket.CONNECTING)
    ) {
      return;
    }

    if (wsRef.current) {
      wsRef.current.onclose = null;
      wsRef.current.close();
      wsRef.current = null;
    }

    setConnectionStatus('connecting');

    const wsBase =
      import.meta.env.VITE_BATTLE_WS_URL ||
      `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}`;
    const wsUrl = `${wsBase}/ws/battle?matchId=${numericMatchId}`;

    try {
      const ws = new WebSocket(wsUrl);
      wsRef.current = ws;

      ws.onopen = () => {
        if (!mountedRef.current) return;
        setConnectionStatus('connected');
        reconnectDelayRef.current = INITIAL_RECONNECT_DELAY;
      };

      ws.onmessage = (event) => {
        if (!mountedRef.current) return;
        try {
          const payload: BattleBarBroadcastPayload = JSON.parse(event.data);
          if (Array.isArray(payload.bars)) {
            const mapped: Record<number, number> = {};
            for (const b of payload.bars) {
              mapped[b.teamId] = b.totalDamage;
            }
            setBars(prev => ({ ...prev, ...mapped }));
          }

          if (payload.latestAttack) {
            setLatestAttack(payload.latestAttack);
            // Clear latest attack indicator after 3 seconds
            setTimeout(() => {
              if (mountedRef.current) {
                setLatestAttack(null);
              }
            }, 3000);
          }
        } catch (err) {
          console.error('Failed to parse battle-bar broadcast frame:', err);
        }
      };

      ws.onerror = () => {
        if (!mountedRef.current) return;
        setConnectionStatus('error');
      };

      ws.onclose = (event) => {
        if (!mountedRef.current) return;
        setConnectionStatus('disconnected');
        wsRef.current = null;

        // Auto-reconnect unless cleanly closed
        if (!event.wasClean) {
          const delay = reconnectDelayRef.current;
          reconnectDelayRef.current = Math.min(delay * 2, MAX_RECONNECT_DELAY);
          reconnectTimerRef.current = setTimeout(() => {
            if (mountedRef.current) {
              connectWebSocket();
            }
          }, delay);
        }
      };
    } catch (err) {
      console.error('WebSocket connection initialization failed:', err);
      if (mountedRef.current) {
        setConnectionStatus('error');
      }
    }
  }, [numericMatchId]);

  useEffect(() => {
    mountedRef.current = true;
    fetchBattleBars();
    connectWebSocket();

    return () => {
      mountedRef.current = false;
      if (reconnectTimerRef.current) {
        clearTimeout(reconnectTimerRef.current);
      }
      if (wsRef.current) {
        wsRef.current.onclose = null;
        wsRef.current.close();
        wsRef.current = null;
      }
    };
  }, [numericMatchId, fetchBattleBars, connectWebSocket]);

  const teamADamage = teamAId ? bars[teamAId] ?? 0 : 0;
  const teamBDamage = teamBId ? bars[teamBId] ?? 0 : 0;

  return {
    bars,
    teamADamage,
    teamBDamage,
    latestAttack,
    connectionStatus,
    isLoading,
    refetch: fetchBattleBars,
  };
}
