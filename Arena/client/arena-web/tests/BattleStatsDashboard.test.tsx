import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import BattleStatsDashboardView, { formatLastAttackAt, formatCompletedAt } from '../src/views/BattleStatsDashboardView';
import { analyticsService } from '../src/api/analyticsService';
import { StreamTeamBattleResponse, StreamBattleDashboardResponse } from '../src/types';

jest.mock('../src/api/analyticsService', () => ({
  analyticsService: {
    getAllBattleSummaries: jest.fn(),
    getStreamBattleDashboard: jest.fn(),
    getRoundOutcomes: jest.fn(),
  },
}));

const mockGetAllBattleSummaries = analyticsService.getAllBattleSummaries as jest.MockedFunction<
  typeof analyticsService.getAllBattleSummaries
>;
const mockGetStreamBattleDashboard = analyticsService.getStreamBattleDashboard as jest.MockedFunction<
  typeof analyticsService.getStreamBattleDashboard
>;

const renderWithRouter = (ui: React.ReactElement) => {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
};

describe('BattleStatsDashboardView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('format helpers', () => {
    it('formatLastAttackAt handles null, empty, and invalid dates', () => {
      expect(formatLastAttackAt(null)).toBe('No attacks yet');
      expect(formatLastAttackAt('')).toBe('No attacks yet');
      expect(formatLastAttackAt('not-a-valid-date')).toBe('No attacks yet');
    });

    it('formatLastAttackAt formats valid dates', () => {
      const formatted = formatLastAttackAt('2026-10-06T12:00:00Z');
      expect(formatted).not.toBe('No attacks yet');
      expect(formatted).not.toContain('Invalid Date');
    });

    it('formatCompletedAt handles null, empty, and invalid timestamps', () => {
      expect(formatCompletedAt(null)).toBe('In progress');
      expect(formatCompletedAt('')).toBe('In progress');
      expect(formatCompletedAt('invalid-timestamp')).toBe('In progress');
    });

    it('formatCompletedAt formats valid timestamps', () => {
      const formatted = formatCompletedAt('2026-10-06T12:30:00Z');
      expect(formatted).not.toBe('In progress');
    });
  });

  it('displays loading state while API requests are pending', () => {
    mockGetAllBattleSummaries.mockReturnValue(new Promise(() => {}));

    renderWithRouter(<BattleStatsDashboardView />);

    expect(screen.getByText('Loading battle analytics...')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('renders error state when fetching battle data fails and retries on button click', async () => {
    mockGetAllBattleSummaries.mockRejectedValueOnce(new Error('Network error'));

    renderWithRouter(<BattleStatsDashboardView />);

    await waitFor(() => {
      expect(screen.queryByText('Loading battle analytics...')).not.toBeInTheDocument();
    });

    expect(screen.getByText('Unable to Load Battle Analytics')).toBeInTheDocument();
    expect(screen.getByText('Failed to load battle analytics data. Please try again.')).toBeInTheDocument();

    // Setup success for retry
    mockGetAllBattleSummaries.mockResolvedValueOnce([]);

    const retryBtn = screen.getByRole('button', { name: /retry/i });
    fireEvent.click(retryBtn);

    await waitFor(() => {
      expect(mockGetAllBattleSummaries).toHaveBeenCalledTimes(2);
    });
  });

  it('renders empty state when no streams have battle data', async () => {
    mockGetAllBattleSummaries.mockResolvedValueOnce([]);

    renderWithRouter(<BattleStatsDashboardView />);

    await waitFor(() => {
      expect(screen.queryByText('Loading battle analytics...')).not.toBeInTheDocument();
    });

    expect(screen.getByText('No battle or attack data yet')).toBeInTheDocument();
    expect(screen.getByText(/Attack metrics and round outcomes will appear here once viewers launch attacks on streams/i)).toBeInTheDocument();
  });

  it('renders complete dashboard with KPI cards, team stats, attack volume bars, and round outcomes', async () => {
    const mockSummaries: StreamTeamBattleResponse[] = [
      {
        streamId: 101,
        teamId: 'team-alpha',
        teamName: 'Alpha Wolves',
        totalAttacks: 200,
        totalDamageDealt: 6000,
        totalCoinsSpent: 1500,
        roundsWon: 2,
        roundsLost: 1,
        lastEventAt: '2026-10-06T11:00:00Z',
      },
      {
        streamId: 102,
        teamId: 'team-gamma',
        teamName: 'Gamma Guardians',
        totalAttacks: 50,
        totalDamageDealt: 1200,
        totalCoinsSpent: 300,
        roundsWon: 1,
        roundsLost: 0,
        lastEventAt: '2026-10-06T10:00:00Z',
      },
    ];

    const mockDashboard101: StreamBattleDashboardResponse = {
      streamId: 101,
      teams: [
        {
          streamId: 101,
          teamId: 'team-alpha',
          teamName: 'Alpha Wolves',
          totalAttacks: 200,
          totalDamageDealt: 6000,
          totalCoinsSpent: 1500,
          roundsWon: 2,
          roundsLost: 1,
          lastEventAt: '2026-10-06T11:00:00Z',
        },
        {
          streamId: 101,
          teamId: 'team-beta',
          teamName: 'Beta Bears',
          totalAttacks: 100,
          totalDamageDealt: 3000,
          totalCoinsSpent: 800,
          roundsWon: 1,
          roundsLost: 2,
          lastEventAt: '2026-10-06T11:05:00Z',
        },
      ],
      rounds: [
        {
          streamId: 101,
          roundNumber: 1,
          winningTeamId: 'team-alpha',
          winningTeamName: 'Alpha Wolves',
          teamAId: 'team-alpha',
          teamBId: 'team-beta',
          teamAAttacks: 80,
          teamBAttacks: 60,
          teamADamage: 2400,
          teamBDamage: 1800,
          completedAt: '2026-10-06T10:30:00Z',
        },
        {
          streamId: 101,
          roundNumber: 2,
          winningTeamId: 'team-beta',
          winningTeamName: 'Beta Bears',
          teamAId: 'team-alpha',
          teamBId: 'team-beta',
          teamAAttacks: 60,
          teamBAttacks: 70,
          teamADamage: 1800,
          teamBDamage: 2100,
          completedAt: '2026-10-06T10:45:00Z',
        },
      ],
    };

    mockGetAllBattleSummaries.mockResolvedValueOnce(mockSummaries);
    mockGetStreamBattleDashboard.mockResolvedValueOnce(mockDashboard101);

    renderWithRouter(<BattleStatsDashboardView />);

    await waitFor(() => {
      expect(screen.queryByText('Loading battle analytics...')).not.toBeInTheDocument();
    });

    // Header & Tab Navigation
    expect(screen.getByText('Battle & Attack Stats Dashboard')).toBeInTheDocument();

    // Stream Selector
    const select = screen.getByLabelText(/filter by stream/i) as HTMLSelectElement;
    expect(select.value).toBe('101');

    // KPI Cards: Total Attacks = 300, Total Damage = 9,000, Coins Spent = 2,300, Rounds = 2
    expect(screen.getByText(/300 attacks/i)).toBeInTheDocument();
    expect(screen.getByText(/9,000 dmg/i)).toBeInTheDocument();
    expect(screen.getByText(/2,300 coins/i)).toBeInTheDocument();
    expect(screen.getByTitle('Rounds')).toHaveTextContent('2 rounds');

    // Team Table
    expect(screen.getAllByText('Alpha Wolves').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Beta Bears').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('200 attacks')).toBeInTheDocument();
    expect(screen.getByText('67%')).toBeInTheDocument();
    expect(screen.getByText('100 attacks')).toBeInTheDocument();
    expect(screen.getByText('33%')).toBeInTheDocument();
    expect(screen.getByText('2 W')).toBeInTheDocument();
    expect(screen.getByText('1 L')).toBeInTheDocument();
    expect(screen.getByText('1 W')).toBeInTheDocument();
    expect(screen.getByText('2 L')).toBeInTheDocument();

    // Round Outcomes Table
    expect(screen.getByText('Round 1')).toBeInTheDocument();
    expect(screen.getByText('80 attacks')).toBeInTheDocument();
    expect(screen.getByText('(2400 dmg)')).toBeInTheDocument();
  });

  it('switches streams when the user selects a different stream', async () => {
    const mockSummaries: StreamTeamBattleResponse[] = [
      {
        streamId: 101,
        teamId: 'team-alpha',
        teamName: 'Alpha Wolves',
        totalAttacks: 200,
        totalDamageDealt: 6000,
        totalCoinsSpent: 1500,
        roundsWon: 2,
        roundsLost: 1,
        lastEventAt: '2026-10-06T11:00:00Z',
      },
      {
        streamId: 102,
        teamId: 'team-gamma',
        teamName: 'Gamma Guardians',
        totalAttacks: 50,
        totalDamageDealt: 1200,
        totalCoinsSpent: 300,
        roundsWon: 1,
        roundsLost: 0,
        lastEventAt: '2026-10-06T10:00:00Z',
      },
    ];

    const mockDashboard101: StreamBattleDashboardResponse = {
      streamId: 101,
      teams: [
        {
          streamId: 101,
          teamId: 'team-alpha',
          teamName: 'Alpha Wolves',
          totalAttacks: 200,
          totalDamageDealt: 6000,
          totalCoinsSpent: 1500,
          roundsWon: 2,
          roundsLost: 1,
          lastEventAt: '2026-10-06T11:00:00Z',
        },
      ],
      rounds: [],
    };

    const mockDashboard102: StreamBattleDashboardResponse = {
      streamId: 102,
      teams: [
        {
          streamId: 102,
          teamId: 'team-gamma',
          teamName: 'Gamma Guardians',
          totalAttacks: 50,
          totalDamageDealt: 1200,
          totalCoinsSpent: 300,
          roundsWon: 1,
          roundsLost: 0,
          lastEventAt: '2026-10-06T10:00:00Z',
        },
      ],
      rounds: [],
    };

    mockGetAllBattleSummaries.mockResolvedValueOnce(mockSummaries);
    mockGetStreamBattleDashboard.mockResolvedValueOnce(mockDashboard101);

    renderWithRouter(<BattleStatsDashboardView />);

    await waitFor(() => {
      expect(screen.getByText('Alpha Wolves')).toBeInTheDocument();
    });

    // Now switch to Stream 102
    mockGetStreamBattleDashboard.mockResolvedValueOnce(mockDashboard102);

    const select = screen.getByLabelText(/filter by stream/i) as HTMLSelectElement;
    fireEvent.change(select, { target: { value: '102' } });

    await waitFor(() => {
      expect(screen.getByText('Gamma Guardians')).toBeInTheDocument();
    });

    expect(select.value).toBe('102');
  });
});
