import { renderHook, act } from '@testing-library/react';
import { useWatchHeartbeat, WATCH_HEARTBEAT_INTERVAL_MS } from '../src/hooks/useWatchHeartbeat';
import { economyService } from '../src/api/economyService';
import { WatchTickResponse } from '../src/types';

jest.mock('../src/api/economyService', () => ({
  economyService: {
    recordWatchTick: jest.fn(),
  },
}));

const mockRecordWatchTick = economyService.recordWatchTick as jest.MockedFunction<
  typeof economyService.recordWatchTick
>;

const mockSuccessResponse: WatchTickResponse = {
  success: true,
  coinsAwarded: 10,
  currentBalance: 150,
  lastTickAt: '2026-09-25T12:00:00Z',
  remainingSeconds: 60,
  message: 'Coins awarded successfully',
};

/** Helper to simulate document visibility changes in jsdom. */
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

describe('useWatchHeartbeat', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    jest.clearAllMocks();
    setDocumentVisibility('visible');
    mockRecordWatchTick.mockResolvedValue(mockSuccessResponse);
  });

  afterEach(() => {
    jest.useRealTimers();
    setDocumentVisibility('visible');
  });

  // ── Existing Step 3 Behaviors ─────────────────────────────────────────────

  it('does not send watch-tick requests while isPlaying is false', async () => {
    renderHook(() => useWatchHeartbeat({ streamId: 101, isPlaying: false }));

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('starts the 60-second heartbeat when isPlaying becomes true', async () => {
    let isPlaying = false;
    const { rerender } = renderHook(() =>
      useWatchHeartbeat({ streamId: 101, isPlaying })
    );

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).not.toHaveBeenCalled();

    isPlaying = true;
    rerender();

    // Before 60 seconds expire, no call yet
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS - 1);
    });
    expect(mockRecordWatchTick).not.toHaveBeenCalled();

    // At 60 seconds, recordWatchTick fires
    await act(async () => {
      jest.advanceTimersByTime(1);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenCalledWith({ streamId: 101 });
  });

  it('calls recordWatchTick at the expected 60-second interval', async () => {
    renderHook(() => useWatchHeartbeat({ streamId: 202, isPlaying: true }));

    // Tick 1
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenLastCalledWith({ streamId: 202 });

    // Tick 2
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);

    // Tick 3
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(3);
  });

  it('stops requests when isPlaying becomes false', async () => {
    let isPlaying = true;
    const { rerender } = renderHook(() =>
      useWatchHeartbeat({ streamId: 303, isPlaying })
    );

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    isPlaying = false;
    rerender();

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
  });

  it('cleans up the timer on unmount', async () => {
    const { unmount } = renderHook(() =>
      useWatchHeartbeat({ streamId: 404, isPlaying: true })
    );

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    unmount();

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 3);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
  });

  it('does not create duplicate timers on rerender', async () => {
    const { rerender } = renderHook(() =>
      useWatchHeartbeat({ streamId: 505, isPlaying: true })
    );

    rerender();
    rerender();
    rerender();

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('handles API rejection safely without throwing unhandled errors', async () => {
    const apiError = new Error('Network failure or unauthorized');
    mockRecordWatchTick.mockRejectedValue(apiError);

    const onError = jest.fn();
    renderHook(() =>
      useWatchHeartbeat({ streamId: 606, isPlaying: true, onError })
    );

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(onError).toHaveBeenCalledWith(apiError);
  });

  it('invokes onSuccess callback on successful watch-tick response', async () => {
    const onSuccess = jest.fn();
    renderHook(() =>
      useWatchHeartbeat({ streamId: 707, isPlaying: true, onSuccess })
    );

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });

    expect(onSuccess).toHaveBeenCalledTimes(1);
    expect(onSuccess).toHaveBeenCalledWith(mockSuccessResponse);
  });

  it('supports positional arguments syntax useWatchHeartbeat(streamId, isPlaying)', async () => {
    renderHook(() => useWatchHeartbeat(808, true));

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenCalledWith({ streamId: 808 });
  });

  it('omits streamId from payload if undefined', async () => {
    renderHook(() => useWatchHeartbeat({ isPlaying: true }));

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenCalledWith(undefined);
  });

  // ── Step 4 Page Visibility API Integration ────────────────────────────────

  it('does not send requests while document is hidden on initial render', async () => {
    setDocumentVisibility('hidden');

    renderHook(() => useWatchHeartbeat({ streamId: 901, isPlaying: true }));

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('pauses an active heartbeat immediately when visibility becomes hidden', async () => {
    renderHook(() => useWatchHeartbeat({ streamId: 902, isPlaying: true }));

    // Tick 1 after 60s while visible
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Switch to hidden
    setDocumentVisibility('hidden');

    // Advance by several intervals while hidden — zero new requests dispatched
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 3);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
  });

  it('resumes heartbeat when visibility changes back to visible without immediate tick', async () => {
    renderHook(() => useWatchHeartbeat({ streamId: 903, isPlaying: true }));

    // Tick 1
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Hide tab
    setDocumentVisibility('hidden');
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Return to visible tab
    setDocumentVisibility('visible');

    // Must NOT send an immediate request merely on becoming visible
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Must wait for the full 60-second interval before next tick
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS - 1);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    await act(async () => {
      jest.advanceTimersByTime(1);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('remains inactive when isPlaying is false even after visibility becomes visible', async () => {
    setDocumentVisibility('hidden');

    renderHook(() => useWatchHeartbeat({ streamId: 904, isPlaying: false }));

    // Becoming visible while isPlaying is false
    setDocumentVisibility('visible');

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS * 2);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not create duplicate timers during repeated visibility changes', async () => {
    renderHook(() => useWatchHeartbeat({ streamId: 905, isPlaying: true }));

    // Rapid visibility toggling
    setDocumentVisibility('hidden');
    setDocumentVisibility('visible');
    setDocumentVisibility('hidden');
    setDocumentVisibility('visible');

    // Advance 60s
    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });

    // Exactly 1 tick should occur, not multiple
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    await act(async () => {
      jest.advanceTimersByTime(WATCH_HEARTBEAT_INTERVAL_MS);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('removes the visibilitychange event listener on unmount', () => {
    const removeEventListenerSpy = jest.spyOn(document, 'removeEventListener');

    const { unmount } = renderHook(() =>
      useWatchHeartbeat({ streamId: 906, isPlaying: true })
    );

    unmount();

    expect(removeEventListenerSpy).toHaveBeenCalledWith(
      'visibilitychange',
      expect.any(Function)
    );

    removeEventListenerSpy.mockRestore();
  });
});
