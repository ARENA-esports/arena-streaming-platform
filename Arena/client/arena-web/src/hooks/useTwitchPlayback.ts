import { useState, useCallback } from 'react';

export interface UseTwitchPlaybackResult {
  /** Whether the Twitch stream is currently playing. */
  isPlaying: boolean;
  /** Callback to invoke when Twitch player fires a PLAY event. */
  handlePlay: () => void;
  /** Callback to invoke when Twitch player fires a PAUSE event. */
  handlePause: () => void;
  /** Alias for handlePlay, suitable for direct prop passing: onPlay={onPlay}. */
  onPlay: () => void;
  /** Alias for handlePause, suitable for direct prop passing: onPause={onPause}. */
  onPause: () => void;
  /** Direct setter for playback state if needed. */
  setIsPlaying: (playing: boolean) => void;
  /** Resets playback state to false. */
  resetPlayback: () => void;
}

/**
 * Reusable hook to track and manage Twitch player playback state.
 * Exposes boolean `isPlaying` and stable event handlers for PLAY/PAUSE.
 * Designed to connect TwitchPlayer/StreamContainer events to useWatchHeartbeat.
 */
export function useTwitchPlayback(initialPlaying = false): UseTwitchPlaybackResult {
  const [isPlaying, setIsPlaying] = useState<boolean>(initialPlaying);

  const handlePlay = useCallback(() => {
    setIsPlaying(true);
  }, []);

  const handlePause = useCallback(() => {
    setIsPlaying(false);
  }, []);

  const resetPlayback = useCallback(() => {
    setIsPlaying(false);
  }, []);

  return {
    isPlaying,
    handlePlay,
    handlePause,
    onPlay: handlePlay,
    onPause: handlePause,
    setIsPlaying,
    resetPlayback,
  };
}
