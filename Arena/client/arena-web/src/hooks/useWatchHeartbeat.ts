import { useEffect, useRef } from 'react';
import { economyService } from '../api/economyService';
import { WatchTickRequest, WatchTickResponse } from '../types';

export const WATCH_HEARTBEAT_INTERVAL_MS = 60000;

export interface UseWatchHeartbeatOptions {
  /** The ID of the stream being watched (optional). */
  streamId?: number | string;
  /** Whether stream playback is currently active. Heartbeat only ticks when true. */
  isPlaying?: boolean;
  /** Heartbeat interval in milliseconds. Defaults to 60,000 (60s). */
  intervalMs?: number;
  /** Callback invoked on successful watch tick. */
  onSuccess?: (response: WatchTickResponse) => void;
  /** Callback invoked when a watch tick request fails. */
  onError?: (error: unknown) => void;
}

/**
 * Core hook for client-side viewer watch heartbeat.
 * Sends POST /api/economy/watch-tick every 60 seconds while playback is active.
 *
 * Supports both object options: `useWatchHeartbeat({ streamId, isPlaying })`
 * and positional arguments: `useWatchHeartbeat(streamId, isPlaying)`.
 */
export function useWatchHeartbeat(
  optionsOrStreamId?: UseWatchHeartbeatOptions | number | string,
  isPlayingArg?: boolean
): void {
  const options: UseWatchHeartbeatOptions =
    typeof optionsOrStreamId === 'object' && optionsOrStreamId !== null
      ? optionsOrStreamId
      : {
          streamId: optionsOrStreamId,
          isPlaying: isPlayingArg,
        };

  const {
    streamId,
    isPlaying = false,
    intervalMs = WATCH_HEARTBEAT_INTERVAL_MS,
    onSuccess,
    onError,
  } = options;

  const onSuccessRef = useRef(onSuccess);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onSuccessRef.current = onSuccess;
  }, [onSuccess]);

  useEffect(() => {
    onErrorRef.current = onError;
  }, [onError]);

  useEffect(() => {
    if (!isPlaying) {
      return;
    }

    const intervalId = setInterval(async () => {
      try {
        const numericStreamId =
          typeof streamId === 'number'
            ? streamId
            : typeof streamId === 'string' &&
              !isNaN(Number(streamId)) &&
              streamId.trim() !== ''
            ? Number(streamId)
            : undefined;

        const payload: WatchTickRequest | undefined =
          numericStreamId !== undefined ? { streamId: numericStreamId } : undefined;

        const response = await economyService.recordWatchTick(payload);
        onSuccessRef.current?.(response);
      } catch (err) {
        // Prevent unhandled promise rejections
        onErrorRef.current?.(err);
      }
    }, intervalMs);

    return () => {
      clearInterval(intervalId);
    };
  }, [isPlaying, streamId, intervalMs]);
}
