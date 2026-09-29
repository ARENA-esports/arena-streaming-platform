import React, { useEffect, useRef, useCallback } from 'react';
import { TwitchPlayerInstance } from '../../types/twitch';
import { useTwitchSdk } from '../../hooks/useTwitchSdk';

// ─── Public interface ────────────────────────────────────────────────────────

export interface TwitchPlayerProps {
  channel: string;
  parentDomains?: string[];
  className?: string;
  /** Called when the Twitch player begins playing. */
  onPlay?: () => void;
  /** Called when the Twitch player is paused. */
  onPause?: () => void;
}

// ─── Resolved parent domains helper ─────────────────────────────────────────

function resolveParentDomains(parentDomains?: string[]): string[] {
  const envDomains = (import.meta.env?.VITE_APP_DOMAIN || '')
    .split(',')
    .map((d: string) => d.trim())
    .filter(Boolean);
  const currentHost =
    typeof window !== 'undefined' ? window.location.hostname : 'localhost';
  return Array.from(
    new Set([
      ...(parentDomains || []),
      ...envDomains,
      currentHost,
      'localhost',
      'arena-esports.azurewebsites.net',
    ])
  );
}

// ─── Component ───────────────────────────────────────────────────────────────

export const TwitchPlayer: React.FC<TwitchPlayerProps> = ({
  channel,
  parentDomains,
  className = '',
  onPlay,
  onPause,
}) => {
  // DOM container the Twitch SDK will render into
  const containerRef = useRef<HTMLDivElement>(null);
  // Stable reference to the live Twitch.Player instance
  const playerRef = useRef<TwitchPlayerInstance | null>(null);
  // Stable callbacks so useEffect deps do not cause recreations
  const onPlayRef = useRef(onPlay);
  const onPauseRef = useRef(onPause);

  useEffect(() => { onPlayRef.current = onPlay; }, [onPlay]);
  useEffect(() => { onPauseRef.current = onPause; }, [onPause]);

  const { sdk, state: sdkState } = useTwitchSdk();

  const trimmedChannel = channel?.trim();

  // ── Internal callbacks (stable identity via ref forwarding) ────────────────
  const handlePlay = useCallback(() => {
    onPlayRef.current?.();
  }, []);

  const handlePause = useCallback(() => {
    onPauseRef.current?.();
  }, []);

  // ── Create / recreate player whenever SDK or channel changes ───────────────
  useEffect(() => {
    if (sdkState !== 'ready' || !sdk || !trimmedChannel || !containerRef.current) {
      return;
    }

    // Clean up any existing player instance before creating a new one
    if (playerRef.current) {
      const prev = playerRef.current;
      const PConst = sdk.Player;
      prev.removeEventListener(PConst.PLAY, handlePlay);
      prev.removeEventListener(PConst.PAUSE, handlePause);
      if (typeof prev.destroy === 'function') {
        prev.destroy();
      }
      playerRef.current = null;
      // Clear the container so the SDK gets a clean element
      containerRef.current.innerHTML = '';
    }

    const resolvedParents = resolveParentDomains(parentDomains);

    const player = new sdk.Player(containerRef.current, {
      channel: trimmedChannel,
      parent: resolvedParents,
      autoplay: true,
      muted: false,
      width: '100%',
      height: '100%',
    });

    player.addEventListener(sdk.Player.PLAY, handlePlay);
    player.addEventListener(sdk.Player.PAUSE, handlePause);
    playerRef.current = player;

    return () => {
      player.removeEventListener(sdk.Player.PLAY, handlePlay);
      player.removeEventListener(sdk.Player.PAUSE, handlePause);
      if (typeof player.destroy === 'function') {
        player.destroy();
      }
      playerRef.current = null;
      if (containerRef.current) {
        containerRef.current.innerHTML = '';
      }
    };
  }, [sdk, sdkState, trimmedChannel, parentDomains, handlePlay, handlePause]);

  // ── Fallback: no channel provided ─────────────────────────────────────────
  if (!trimmedChannel) {
    return (
      <div
        className={`w-full aspect-video bg-arena-bg border border-arena-border rounded-sm flex flex-col items-center justify-center text-gray-400 ${className}`}
      >
        <p className="text-sm font-medium">No active broadcast selected</p>
        <p className="text-xs text-gray-500 mt-1">
          Select a tournament match to view stream
        </p>
      </div>
    );
  }

  return (
    <div
      className={`relative w-full aspect-video bg-black rounded-sm overflow-hidden border border-arena-border ${className}`}
    >
      {/* SDK loading/error overlays */}
      {sdkState === 'loading' && (
        <div className="absolute inset-0 flex items-center justify-center bg-black/70 z-10">
          <div className="w-8 h-8 border-4 border-arena-cyan border-t-transparent rounded-full animate-spin" />
        </div>
      )}
      {sdkState === 'error' && (
        <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/80 z-10 text-gray-400">
          <p className="text-sm font-medium">Could not load Twitch player</p>
          <p className="text-xs text-gray-500 mt-1">
            Please disable ad-blocker or tracking prevention for this site.
          </p>
        </div>
      )}
      {/* Twitch SDK mounts the iframe here */}
      <div ref={containerRef} className="absolute inset-0 w-full h-full" />
    </div>
  );
};

export default TwitchPlayer;