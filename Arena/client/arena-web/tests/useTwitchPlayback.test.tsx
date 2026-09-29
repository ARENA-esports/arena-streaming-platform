import React from 'react';
import { render, renderHook, act, waitFor } from '@testing-library/react';
import { useTwitchPlayback } from '../src/hooks/useTwitchPlayback';
import { useWatchHeartbeat, WATCH_HEARTBEAT_INTERVAL_MS } from '../src/hooks/useWatchHeartbeat';
import { TwitchPlayer } from '../src/components/player/TwitchPlayer';
import { economyService } from '../src/api/economyService';
import { TwitchPlayerInstance, TwitchSdk } from '../src/types/twitch';

jest.mock('../src/api/economyService', () => ({
  economyService: {
    recordWatchTick: jest.fn(),
  },
}));

const mockRecordWatchTick = economyService.recordWatchTick as jest.MockedFunction<
  typeof economyService.recordWatchTick
>;

// ── Helpers ──────────────────────────────────────────────────────────────────

function setDocumentVisibility(state: 'visible' | 'hidden') {
  Object.defineProperty(document, 'visibilityState', {
    configurable: true,
    value: state,
    writable: true,
  });
  act(() => {
    document.dispatchEvent(new Event('visibilitychange'));
  });
}

type EventName = string;
type EventCallback = () => void;

function createMockPlayerInstance() {
  const callbacks = new Map<EventName, EventCallback[]>();
  return {
    _callbacks: callbacks,
    _fire(event: EventName) {
      (callbacks.get(event) || []).forEach((cb) => cb());
    },
    addEventListener: jest.fn((event: EventName, cb: EventCallback) => {
      const existing = callbacks.get(event) || [];
      callbacks.set(event, [...existing, cb]);
    }),
    removeEventListener: jest.fn((event: EventName, cb: EventCallback) => {
      const existing = callbacks.get(event) || [];
      callbacks.set(event, existing.filter((c) => c !== cb));
    }),
    play: jest.fn(),
    pause: jest.fn(),
    setChannel: jest.fn(),
    getChannel: jest.fn(() => 'shroud'),
    isPaused: jest.fn(() => false),
    destroy: jest.fn(),
  };
}

function installMockSdk(playerFactory?: () => TwitchPlayerInstance) {
  const MockPlayer = jest.fn((
    _element: string | HTMLElement,
    _options: Record<string, unknown>
  ) => (playerFactory ? playerFactory() : createMockPlayerInstance())) as unknown as jest.Mock & {
    PLAY: string;
    PAUSE: string;
    READY: string;
    ONLINE: string;
    OFFLINE: string;
    ENDED: string;
  };

  MockPlayer.PLAY = 'play';
  MockPlayer.PAUSE = 'pause';
  MockPlayer.READY = 'ready';
  MockPlayer.ONLINE = 'online';
  MockPlayer.OFFLINE = 'offline';
  MockPlayer.ENDED = 'ended';

  const mockSdk = { Player: MockPlayer } as unknown as TwitchSdk;
  window.Twitch = mockSdk;

  const script = document.getElementById('twitch-player-sdk') as HTMLScriptElement | null;
  if (script) {
    act(() => {
      script.dispatchEvent(new Event('load'));
    });
  }

  return { MockPlayer, mockSdk };
}

describe('useTwitchPlayback hook', () => {
  it('initializes with isPlaying = false by default', () => {
    const { result } = renderHook(() => useTwitchPlayback());
    expect(result.current.isPlaying).toBe(false);
  });

  it('respects custom initial state if provided', () => {
    const { result } = renderHook(() => useTwitchPlayback(true));
    expect(result.current.isPlaying).toBe(true);
  });

  it('sets isPlaying to true when handlePlay or onPlay is called', () => {
    const { result } = renderHook(() => useTwitchPlayback());

    act(() => {
      result.current.handlePlay();
    });
    expect(result.current.isPlaying).toBe(true);

    act(() => {
      result.current.handlePause();
    });
    expect(result.current.isPlaying).toBe(false);

    act(() => {
      result.current.onPlay();
    });
    expect(result.current.isPlaying).toBe(true);
  });

  it('sets isPlaying to false when handlePause or onPause is called', () => {
    const { result } = renderHook(() => useTwitchPlayback(true));

    act(() => {
      result.current.handlePause();
    });
    expect(result.current.isPlaying).toBe(false);

    act(() => {
      result.current.onPlay();
    });
    expect(result.current.isPlaying).toBe(true);

    act(() => {
      result.current.onPause();
    });
    expect(result.current.isPlaying).toBe(false);
  });

  it('resets isPlaying to false when resetPlayback is called', () => {
    const { result } = renderHook(() => useTwitchPlayback(true));

    act(() => {
      result.current.resetPlayback();
    });
    expect(result.current.isPlaying).toBe(false);
  });

  it('maintains referentially stable callbacks across re-renders', () => {
    const { result, rerender } = renderHook(() => useTwitchPlayback());

    const firstHandlePlay = result.current.handlePlay;
    const firstHandlePause = result.current.handlePause;
    const firstOnPlay = result.current.onPlay;
    const firstOnPause = result.current.onPause;

    rerender();

    expect(result.current.handlePlay).toBe(firstHandlePlay);
    expect(result.current.handlePause).toBe(firstHandlePause);
    expect(result.current.onPlay).toBe(firstOnPlay);
    expect(result.current.onPause).toBe(firstOnPause);
  });
});

describe('Twitch Playback → Heartbeat Integration', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    jest.clearAllMocks();
    setDocumentVisibility('visible');
    const existing = document.getElementById('twitch-player-sdk');
    if (existing) existing.remove();
    delete window.Twitch;
    mockRecordWatchTick.mockResolvedValue({
      success: true,
      coinsAwarded: 10,
      currentBalance: 100,
      lastTickAt: '2026-09-25T12:00:00Z',
      message: 'Coins awarded',
    });
  });

  afterEach(() => {
    jest.useRealTimers();
    setDocumentVisibility('visible');
  });

  // Test component that models the integration pattern
  function StreamWatcherIntegration({ streamId = 42, channel = 'shroud' }) {
    const { isPlaying, onPlay, onPause } = useTwitchPlayback();
    useWatchHeartbeat({ streamId, isPlaying });

    return <TwitchPlayer channel={channel} onPlay={onPlay} onPause={onPause} />;
  }

  it('integrates Twitch PLAY/PAUSE events with useWatchHeartbeat gating', async () => {
    const mockInstance = createMockPlayerInstance();
    const { MockPlayer } = installMockSdk(() => mockInstance);

    render(<StreamWatcherIntegration streamId={777} channel="shroud" />);

    // Wait for player to initialize
    await waitFor(() => {
      expect(MockPlayer).toHaveBeenCalledTimes(1);
    });

    // 1. Initially paused: advance 60s -> no watch-tick
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });
    expect(mockRecordWatchTick).not.toHaveBeenCalled();

    // 2. Twitch player fires PLAY -> isPlaying = true
    act(() => {
      mockInstance._fire('play');
    });

    // Before 60s expires: no tick yet
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS - 1);
    });
    expect(mockRecordWatchTick).not.toHaveBeenCalled();

    // At 60s: watch tick fires
    await act(async () => {
      jest.advanceTimersByTime(1);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenCalledWith({ streamId: 777 });

    // 3. Twitch player fires PAUSE -> isPlaying = false
    act(() => {
      mockInstance._fire('pause');
    });

    // Advance while paused: zero new ticks
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 3);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // 4. Twitch player resumes PLAY -> heartbeat resumes after 60s
    act(() => {
      mockInstance._fire('play');
    });

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('pauses heartbeat when tab is hidden even while Twitch is playing', async () => {
    const mockInstance = createMockPlayerInstance();
    installMockSdk(() => mockInstance);

    render(<StreamWatcherIntegration streamId={888} channel="shroud" />);

    await waitFor(() => {
      expect(mockInstance.addEventListener).toHaveBeenCalled();
    });

    // Start playing
    act(() => {
      mockInstance._fire('play');
    });

    // Tick 1 after 60s
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Switch tab to hidden while still playing
    setDocumentVisibility('hidden');

    // 120s while hidden -> zero ticks
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Return to visible tab
    setDocumentVisibility('visible');

    // Resumes after 60s
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('does not recreate Twitch Player instance on playback state updates', async () => {
    const mockInstance = createMockPlayerInstance();
    const { MockPlayer } = installMockSdk(() => mockInstance);

    render(<StreamWatcherIntegration streamId={999} channel="shroud" />);

    await waitFor(() => {
      expect(MockPlayer).toHaveBeenCalledTimes(1);
    });

    // Multiple play/pause toggles
    act(() => { mockInstance._fire('play'); });
    act(() => { mockInstance._fire('pause'); });
    act(() => { mockInstance._fire('play'); });

    // Player should NOT have been re-instantiated
    expect(MockPlayer).toHaveBeenCalledTimes(1);
  });
});
