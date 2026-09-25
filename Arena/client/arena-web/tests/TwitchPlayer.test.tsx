/**
 * Tests for the Twitch Player JS SDK bridge (TwitchPlayer + useTwitchSdk).
 *
 * Mocks the global window.Twitch SDK rather than loading a real script.
 * Does NOT test heartbeat timers, visibilityState, or watch-tick requests —
 * those belong to later SCRUM-113 steps.
 */

import React from 'react';
import { render, act, waitFor } from '@testing-library/react';
import { TwitchPlayer } from '../src/components/player/TwitchPlayer';
import { TwitchPlayerInstance, TwitchSdk } from '../src/types/twitch';

// ── Helpers ──────────────────────────────────────────────────────────────────

type EventName = string;
type EventCallback = () => void;

/** Creates a minimal mock Twitch.Player instance. */
function createMockPlayerInstance(): TwitchPlayerInstance & {
  _callbacks: Map<EventName, EventCallback[]>;
  _fire: (event: EventName) => void;
} {
  const callbacks = new Map<EventName, EventCallback[]>();

  const instance = {
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

  return instance;
}

/** Installs a mock Twitch SDK on window and simulates the script load event. */
function installMockSdk(playerFactory?: () => TwitchPlayerInstance) {
  const MockPlayer = jest.fn((
    _element: string | HTMLElement,
    _options: Record<string, unknown>
  ) => playerFactory ? playerFactory() : createMockPlayerInstance()) as unknown as jest.Mock & {
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

  // Dispatch the load event on the injected script tag if it exists
  const script = document.getElementById('twitch-player-sdk') as HTMLScriptElement | null;
  if (script) {
    act(() => { script.dispatchEvent(new Event('load')); });
  }

  return { MockPlayer, mockSdk };
}

// ── Setup / teardown ─────────────────────────────────────────────────────────

beforeEach(() => {
  // Remove any previously injected SDK script to isolate test state
  const existing = document.getElementById('twitch-player-sdk');
  if (existing) existing.remove();
  delete window.Twitch;
});

afterEach(() => {
  jest.clearAllMocks();
});

// ── Tests ────────────────────────────────────────────────────────────────────

describe('TwitchPlayer — bridge foundation', () => {
  it('renders a loading spinner while the SDK is loading', () => {
    // SDK is not yet on window; script will be injected
    const { container } = render(<TwitchPlayer channel="shroud" />);
    const spinner = container.querySelector('.animate-spin');
    // SDK state starts as 'loading' (script was injected but not yet loaded)
    expect(spinner).toBeInTheDocument();
  });

  it('renders fallback placeholder when channel is empty', () => {
    const { getByText } = render(<TwitchPlayer channel="" />);
    expect(getByText('No active broadcast selected')).toBeInTheDocument();
  });

  it('initialises Twitch.Player after SDK loads', async () => {
    const mockInstance = createMockPlayerInstance();
    const { MockPlayer } = installMockSdk(() => mockInstance);

    const { rerender } = render(<TwitchPlayer channel="shroud" />);

    await waitFor(() => {
      expect(MockPlayer).toHaveBeenCalledTimes(1);
    });

    const [, options] = MockPlayer.mock.calls[0] as [unknown, Record<string, unknown>];
    expect(options.channel).toBe('shroud');
    expect(options.autoplay).toBe(true);

    // Force re-render — no duplicate initialisation should occur
    rerender(<TwitchPlayer channel="shroud" />);
    await waitFor(() => {
      expect(MockPlayer).toHaveBeenCalledTimes(1);
    });
  });

  it('registers PLAY event listener after SDK is ready', async () => {
    const mockInstance = createMockPlayerInstance();
    installMockSdk(() => mockInstance);

    const onPlay = jest.fn();
    render(<TwitchPlayer channel="shroud" onPlay={onPlay} />);

    await waitFor(() => {
      expect(mockInstance.addEventListener).toHaveBeenCalledWith('play', expect.any(Function));
    });
  });

  it('registers PAUSE event listener after SDK is ready', async () => {
    const mockInstance = createMockPlayerInstance();
    installMockSdk(() => mockInstance);

    const onPause = jest.fn();
    render(<TwitchPlayer channel="shroud" onPause={onPause} />);

    await waitFor(() => {
      expect(mockInstance.addEventListener).toHaveBeenCalledWith('pause', expect.any(Function));
    });
  });

  it('forwards PLAY events to the onPlay callback', async () => {
    const mockInstance = createMockPlayerInstance();
    installMockSdk(() => mockInstance);

    const onPlay = jest.fn();
    render(<TwitchPlayer channel="shroud" onPlay={onPlay} />);

    await waitFor(() => {
      expect(mockInstance.addEventListener).toHaveBeenCalledWith('play', expect.any(Function));
    });

    act(() => { mockInstance._fire('play'); });
    expect(onPlay).toHaveBeenCalledTimes(1);
  });

  it('forwards PAUSE events to the onPause callback', async () => {
    const mockInstance = createMockPlayerInstance();
    installMockSdk(() => mockInstance);

    const onPause = jest.fn();
    render(<TwitchPlayer channel="shroud" onPause={onPause} />);

    await waitFor(() => {
      expect(mockInstance.addEventListener).toHaveBeenCalledWith('pause', expect.any(Function));
    });

    act(() => { mockInstance._fire('pause'); });
    expect(onPause).toHaveBeenCalledTimes(1);
  });

  it('removes event listeners and calls destroy() on unmount', async () => {
    const mockInstance = createMockPlayerInstance();
    installMockSdk(() => mockInstance);

    const { unmount } = render(<TwitchPlayer channel="shroud" />);

    await waitFor(() => {
      expect(mockInstance.addEventListener).toHaveBeenCalled();
    });

    unmount();

    expect(mockInstance.removeEventListener).toHaveBeenCalledWith('play', expect.any(Function));
    expect(mockInstance.removeEventListener).toHaveBeenCalledWith('pause', expect.any(Function));
    expect(mockInstance.destroy).toHaveBeenCalledTimes(1);
  });

  it('creates a new player instance when the channel prop changes', async () => {
    const mockInstance1 = createMockPlayerInstance();
    const mockInstance2 = createMockPlayerInstance();
    let callCount = 0;
    const { MockPlayer } = installMockSdk(() => {
      callCount++;
      return callCount === 1 ? mockInstance1 : mockInstance2;
    });

    const { rerender } = render(<TwitchPlayer channel="shroud" />);

    await waitFor(() => {
      expect(MockPlayer).toHaveBeenCalledTimes(1);
    });

    // Switching channel should destroy old instance and create a new one
    rerender(<TwitchPlayer channel="riotgames" />);

    await waitFor(() => {
      expect(MockPlayer).toHaveBeenCalledTimes(2);
    });

    expect(mockInstance1.destroy).toHaveBeenCalledTimes(1);
    const [, options2] = MockPlayer.mock.calls[1] as [unknown, Record<string, unknown>];
    expect(options2.channel).toBe('riotgames');
  });
});
