import React from 'react';
import { render, act, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { MatchRoomView } from '../src/views/MatchRoomView';
import { useAuth } from '../src/context/AuthContext';
import { useMatchStatus } from '../src/hooks/useMatchStatus';
import { economyService } from '../src/api/economyService';

// ── Mocks ────────────────────────────────────────────────────────────────────

jest.mock('../src/context/AuthContext', () => ({
  useAuth: jest.fn(),
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
const mockUseMatchStatus = useMatchStatus as jest.MockedFunction<typeof useMatchStatus>;
const mockRecordWatchTick = economyService.recordWatchTick as jest.MockedFunction<
  typeof economyService.recordWatchTick
>;

function renderMatchRoom(matchId = '101') {
  return render(
    <MemoryRouter initialEntries={[`/matches/${matchId}`]}>
      <Routes>
        <Route path="/matches/:matchId" element={<MatchRoomView />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('MatchRoomView — Watch Heartbeat Integration', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    jest.clearAllMocks();
    mockRecordWatchTick.mockResolvedValue({
      success: true,
      coinsAwarded: 10,
      currentBalance: 200,
      lastTickAt: '2026-09-25T12:00:00Z',
      message: 'Coins awarded',
    });
  });

  afterEach(() => {
    jest.useRealTimers();
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
});
