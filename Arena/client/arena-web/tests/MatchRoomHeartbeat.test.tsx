import React from 'react';
import { render, act, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { MatchRoomView } from '../src/views/MatchRoomView';
import { useAuth } from '../src/context/AuthContext';
import { useWallet } from '../src/context/WalletContext';
import { useMatchStatus } from '../src/hooks/useMatchStatus';
import { economyService } from '../src/api/economyService';
import { useNotification } from '../src/context/NotificationContext';

// ── Mocks ────────────────────────────────────────────────────────────────────

jest.mock('../src/context/AuthContext', () => ({
  useAuth: jest.fn(),
}));

jest.mock('../src/context/WalletContext', () => ({
  useWallet: jest.fn(),
}));

jest.mock('../src/context/NotificationContext', () => ({
  useNotification: jest.fn(),
}));

jest.mock('../src/hooks/useMatchStatus', () => ({
  useMatchStatus: jest.fn(),
}));

jest.mock('../src/api/economyService', () => ({
  economyService: {
    recordWatchTick: jest.fn(),
  },
}));

// Mock StreamContainer to directly expose playback controls
jest.mock('../src/components/player/StreamContainer', () => ({
  StreamContainer: ({
    onPlay,
    onPause,
  }: {
    apiChannelName?: string;
    onPlay?: () => void;
    onPause?: () => void;
  }) => (
    <div data-testid="stream-container">
      <button data-testid="stream-play-btn" onClick={onPlay}>Play</button>
      <button data-testid="stream-pause-btn" onClick={onPause}>Pause</button>
    </div>
  ),
}));

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;
const mockUseWallet = useWallet as jest.MockedFunction<typeof useWallet>;
const mockUseMatchStatus = useMatchStatus as jest.MockedFunction<typeof useMatchStatus>;
const mockUseNotification = useNotification as jest.MockedFunction<typeof useNotification>;
const mockRecordWatchTick = economyService.recordWatchTick as jest.MockedFunction<
  typeof economyService.recordWatchTick
>;
const mockNotify = jest.fn();

function renderMatchRoom(matchId = '101') {
  return render(
    <MemoryRouter initialEntries={[`/matches/${matchId}`]}>
      <Routes>
        <Route path="/matches/:matchId" element={<MatchRoomView />} />
      </Routes>
    </MemoryRouter>
  );
}

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

describe('MatchRoomView — Watch Heartbeat Integration', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    jest.clearAllMocks();
    setDocumentVisibility('visible');
    mockRecordWatchTick.mockResolvedValue({
      success: true,
      coinsAwarded: 10,
      currentBalance: 200,
      lastTickAt: '2026-09-25T12:00:00Z',
      message: 'Coins awarded',
    });
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
    });
    mockNotify.mockClear();
    mockUseNotification.mockReturnValue({
      notifications: [],
      notify: mockNotify,
      dismiss: jest.fn(),
    });
  });

  afterEach(() => {
    jest.useRealTimers();
    setDocumentVisibility('visible');
  });

  it('wires heartbeat with the correct stream ID when an authenticated viewer watches a Live match', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');

    // Initially, stream is mounted but not playing -> 0 ticks
    await act(async () => {
      jest.advanceTimersByTime(60000 * 2);
    });
    expect(mockRecordWatchTick).not.toHaveBeenCalled();

    // Trigger Play from StreamContainer
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    // Advance 60s
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    // Heartbeat fired with the real stream.id (789)
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenCalledWith({ streamId: 789 });
  });

  it('suspends heartbeat when stream pauses and resumes on replay', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');

    // Play stream
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Pause stream
    act(() => {
      fireEvent.click(getByTestId('stream-pause-btn'));
    });

    // Advance 120s while paused -> zero new ticks
    await act(async () => {
      jest.advanceTimersByTime(120000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Resume play -> tick after 60s
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('does not activate heartbeat for non-viewers (e.g. Organizer or Streamer)', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 2,
        username: 'OrganizerUser',
        email: 'organizer@test.com',
        role: 'Organizer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');

    // Even if player begins playback
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not activate heartbeat for user with Streamer role', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 3,
        username: 'StreamerUser',
        email: 'streamer@test.com',
        role: 'Streamer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not activate heartbeat for user with Admin role', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 4,
        username: 'AdminUser',
        email: 'admin@test.com',
        role: 'Admin' as any,
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not activate heartbeat for user with missing or unrecognized role', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 5,
        username: 'UnknownUser',
        email: 'unknown@test.com',
        role: 'Guest' as any,
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('suspends heartbeat when tab becomes hidden and resumes when visible', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    // Advance 60s while visible -> tick 1
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Switch tab to hidden
    setDocumentVisibility('hidden');

    // Advance 120s while hidden -> zero new ticks
    await act(async () => {
      jest.advanceTimersByTime(120000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Return to visible tab
    setDocumentVisibility('visible');

    // Advance 60s -> tick 2
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
  });

  it('does not activate heartbeat when user is unauthenticated', async () => {
    mockUseAuth.mockReturnValue({
      user: null,
      token: null,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 2);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not activate heartbeat when match is not live (e.g. Scheduled)', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Scheduled',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Scheduled',
      },
      stream: null,
      error: null,
    });

    renderMatchRoom('101');

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('cleans up heartbeat timer on unmount', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId, unmount } = renderMatchRoom('101');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);

    // Unmount view (e.g., viewer navigates away)
    unmount();

    await act(async () => {
      jest.advanceTimersByTime(60000 * 2);
    });

    // Zero additional ticks after unmount
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
  });

  it('does not activate heartbeat if stream.id is missing or unavailable even when match is Live', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: null, // Stream not yet linked or resolved
      error: null,
    });

    const { getByTestId } = renderMatchRoom('101');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('updates wallet balance when watch tick succeeds', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    const { getByTestId } = renderMatchRoom('101');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockUpdateBalance).toHaveBeenCalledWith(200);
  });

  // ── SCRUM-114 / SCRUM-115 / SCRUM-118 Error Handling ────────────────────────

  const setupLiveViewer = () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        id: 789,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });
  };

  const createAxiosError = (status: number, data: any, headers: Record<string, string> = {}) => {
    const error = new Error(`Request failed with status code ${status}`) as any;
    error.isAxiosError = true;
    error.response = {
      status,
      data,
      headers,
    };
    return error;
  };

  it('SCRUM-114: silently ignores minimum interval 429 without toast or balance update', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(
        429,
        {
          success: false,
          coinsAwarded: 0,
          currentBalance: 100,
          remainingSeconds: 45,
          message: 'Minimum interval has not elapsed since the last watch tick.',
        },
        { 'retry-after': '45' }
      )
    );

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockNotify).not.toHaveBeenCalled();
    expect(mockUpdateBalance).not.toHaveBeenCalled();
  });

  it('SCRUM-115: displays informational notification on coin cap 429 without updating wallet', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(429, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 100,
        message: 'Coin cap reached for this stream window.',
      })
    );

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockNotify).toHaveBeenCalledWith('Coin cap reached for this stream window.', 'info');
    expect(mockUpdateBalance).not.toHaveBeenCalled();
  });

  it('SCRUM-115: debounces coin cap notification so it is displayed only once during sustained cap', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(429, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 100,
        message: 'Coin cap reached for this stream window.',
      })
    );

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    // Tick 1 (60s) -> Cap error -> notification shown
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockNotify).toHaveBeenCalledTimes(1);

    // Tick 2 (120s) -> Cap error continues -> NO duplicate notification
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockRecordWatchTick).toHaveBeenCalledTimes(2);
    expect(mockNotify).toHaveBeenCalledTimes(1);
    expect(mockUpdateBalance).not.toHaveBeenCalled();
  });

  it('SCRUM-115: resets coin cap guard when a subsequent watch tick awards coins', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    // Tick 1: Cap reached
    mockRecordWatchTick.mockRejectedValueOnce(
      createAxiosError(429, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 100,
        message: 'Coin cap reached for this stream window.',
      })
    );

    // Tick 2: Cap window rolled over, successful tick awards coins
    mockRecordWatchTick.mockResolvedValueOnce({
      success: true,
      coinsAwarded: 10,
      currentBalance: 110,
      lastTickAt: '2026-09-25T12:02:00Z',
      message: 'Coins awarded',
    });

    // Tick 3: Cap reached again
    mockRecordWatchTick.mockRejectedValueOnce(
      createAxiosError(429, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 110,
        message: 'Coin cap reached for this stream window.',
      })
    );

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    // Tick 1 -> Cap toast shown (1)
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockNotify).toHaveBeenCalledTimes(1);

    // Tick 2 -> Success, balance updated, guard reset
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockUpdateBalance).toHaveBeenCalledWith(110);

    // Tick 3 -> Cap toast shown again (2)
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });
    expect(mockNotify).toHaveBeenCalledTimes(2);
  });

  it('SCRUM-118: displays informational notification on stream not live 400 without updating wallet', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(400, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 100,
        message: 'Stream is not currently live.',
      })
    );

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockNotify).toHaveBeenCalledWith('Stream is not currently live.', 'info');
    expect(mockUpdateBalance).not.toHaveBeenCalled();
  });

  it('SCRUM-118: does not display notification for unrelated 400 errors (e.g. invalid stream ID)', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(400, {
        message: 'Invalid stream ID. Must be a positive integer.',
      })
    );

    const { getByTestId } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockNotify).not.toHaveBeenCalled();
    expect(mockUpdateBalance).not.toHaveBeenCalled();
  });

  // ── WatchRewardStatus UI Removal (BUG 01) ───────────────────────────────────

  it('does not render WatchRewardStatus badge in Match Overview even when playback and cap state change', async () => {
    setupLiveViewer();
    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    const { getByTestId, queryByTestId, queryByText } = renderMatchRoom('101');

    // Before playback: no reward status button/badge
    expect(queryByTestId('watch-reward-status')).not.toBeInTheDocument();
    expect(queryByText('Rewards Paused')).not.toBeInTheDocument();

    // Start playback: still no reward status badge
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });
    expect(queryByTestId('watch-reward-status')).not.toBeInTheDocument();
    expect(queryByText('Earning Coins')).not.toBeInTheDocument();

    // Next tick fails with SCRUM-115 cap rejection
    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(429, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 100,
        message: 'Coin cap reached for this stream window.',
      })
    );

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockNotify).toHaveBeenCalledWith('Coin cap reached for this stream window.', 'info');
    expect(queryByTestId('watch-reward-status')).not.toBeInTheDocument();
    expect(queryByText('Reward Cap Reached')).not.toBeInTheDocument();
  });

  it('does not render WatchRewardStatus badge when SCRUM-118 reports stream is not live', async () => {
    setupLiveViewer();
    mockRecordWatchTick.mockRejectedValue(
      createAxiosError(400, {
        success: false,
        coinsAwarded: 0,
        currentBalance: 100,
        message: 'Stream is not currently live.',
      })
    );

    const { getByTestId, queryByTestId, queryByText } = renderMatchRoom('101');
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockNotify).toHaveBeenCalledWith('Stream is not currently live.', 'info');
    expect(queryByTestId('watch-reward-status')).not.toBeInTheDocument();
    expect(queryByText('Rewards Inactive')).not.toBeInTheDocument();
  });

  it('supports backend stream response using streamId property', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        streamId: 555,
        matchId: 101,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const mockUpdateBalance = jest.fn();
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
    });

    const { getByTestId, queryByTestId } = renderMatchRoom('101');

    // No reward status badge in Match Overview
    expect(queryByTestId('watch-reward-status')).not.toBeInTheDocument();

    // Play stream
    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    // 60s passes -> dispatches tick with streamId: 555
    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledWith({ streamId: 555 });
    expect(mockUpdateBalance).toHaveBeenCalledWith(200);
  });

  it('does not activate heartbeat while match or stream data is still loading', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'loading',
      match: null,
      stream: null,
      error: null,
    });

    renderMatchRoom('101');

    await act(async () => {
      jest.advanceTimersByTime(60000 * 2);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not activate heartbeat when stream response fails with an error', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 101,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: null,
      error: new Error('StreamService network error'),
    });

    const { getByTestId } = renderMatchRoom('101');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 2);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('does not activate heartbeat for a Live match that genuinely has no linked stream', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId: 202,
        teamAId: 3,
        teamBId: 4,
        scheduledTime: '2026-09-25T14:00:00Z',
        status: 'Live',
      },
      stream: null,
      error: null,
    });

    const { getByTestId } = renderMatchRoom('202');

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000 * 3);
    });

    expect(mockRecordWatchTick).not.toHaveBeenCalled();
  });

  it('uses the stream-service ID and never substitutes the match ID in watch-tick requests', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        userId: 1,
        username: 'ViewerUser',
        email: 'viewer@test.com',
        role: 'Viewer',
      },
      token: 'jwt-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    const matchId = 101;
    const streamServiceId = 789;

    mockUseMatchStatus.mockReturnValue({
      status: 'Live',
      match: {
        matchId,
        teamAId: 1,
        teamBId: 2,
        scheduledTime: '2026-09-25T12:00:00Z',
        status: 'Live',
      },
      stream: {
        streamId: streamServiceId,
        matchId,
        channelName: 'Arena_streams',
        status: 'Live',
        twitchUrl: 'https://twitch.tv/Arena_streams',
      },
      error: null,
    });

    const { getByTestId } = renderMatchRoom(String(matchId));

    act(() => {
      fireEvent.click(getByTestId('stream-play-btn'));
    });

    await act(async () => {
      jest.advanceTimersByTime(60000);
    });

    expect(mockRecordWatchTick).toHaveBeenCalledTimes(1);
    expect(mockRecordWatchTick).toHaveBeenCalledWith({ streamId: streamServiceId });
    expect(mockRecordWatchTick).not.toHaveBeenCalledWith({ streamId: matchId });
  });
});
