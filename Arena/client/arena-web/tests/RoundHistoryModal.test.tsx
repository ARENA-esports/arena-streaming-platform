import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { RoundHistoryModal } from '../src/components/match/RoundHistoryModal';
import { weaponShopService } from '../src/api/weaponShopService';
import { BattleRoundHistoryDto } from '../src/types';

jest.mock('../src/api/weaponShopService');

const mockGetRoundHistory = weaponShopService.getRoundHistory as jest.MockedFunction<
  typeof weaponShopService.getRoundHistory
>;

describe('RoundHistoryModal Component', () => {
  const mockOnClose = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('does not render anything when isOpen is false', () => {
    const { container } = render(
      <RoundHistoryModal matchId={1} isOpen={false} onClose={mockOnClose} />
    );
    expect(container.firstChild).toBeNull();
  });

  test('renders empty state when no completed rounds exist (AC2)', async () => {
    mockGetRoundHistory.mockResolvedValueOnce([]);

    render(<RoundHistoryModal matchId={1} isOpen={true} onClose={mockOnClose} />);

    expect(screen.getByText(/loading round log\.\.\./i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('No Completed Rounds Yet')).toBeInTheDocument();
    });

    expect(screen.getByText(/round 1 is currently in progress/i)).toBeInTheDocument();
  });

  test('renders round history list in reverse chronological order with winner and final bar state (AC1)', async () => {
    const mockData: BattleRoundHistoryDto[] = [
      {
        roundId: 102,
        matchId: 1,
        roundNumber: 2,
        winningTeamId: 1,
        targetDamage: 100,
        finalBarState: [
          { teamId: 1, totalDamage: 105 },
          { teamId: 2, totalDamage: 80 },
        ],
        createdAt: '2026-10-07T12:00:00Z',
        endedAt: '2026-10-07T12:05:00Z',
      },
      {
        roundId: 101,
        matchId: 1,
        roundNumber: 1,
        winningTeamId: 2,
        targetDamage: 100,
        finalBarState: [
          { teamId: 1, totalDamage: 70 },
          { teamId: 2, totalDamage: 100 },
        ],
        createdAt: '2026-10-07T11:50:00Z',
        endedAt: '2026-10-07T11:55:00Z',
      },
    ];

    mockGetRoundHistory.mockResolvedValueOnce(mockData);

    const teamsMap = {
      1: { name: 'Red Dragons', color: '#EF4444' },
      2: { name: 'Blue Phoenix', color: '#00B8FC' },
    };

    render(
      <RoundHistoryModal
        matchId={1}
        isOpen={true}
        onClose={mockOnClose}
        teamsMap={teamsMap}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Round 2')).toBeInTheDocument();
      expect(screen.getByText('Round 1')).toBeInTheDocument();
    });

    expect(screen.getByText('Winner: Red Dragons')).toBeInTheDocument();
    expect(screen.getByText('Winner: Blue Phoenix')).toBeInTheDocument();

    expect(screen.getByText('105 DMG')).toBeInTheDocument();
    expect(screen.getByText('80 DMG')).toBeInTheDocument();
  });

  test('calls onClose when close button is clicked', async () => {
    mockGetRoundHistory.mockResolvedValueOnce([]);

    render(<RoundHistoryModal matchId={1} isOpen={true} onClose={mockOnClose} />);

    await waitFor(() => {
      expect(screen.getByText('No Completed Rounds Yet')).toBeInTheDocument();
    });

    const closeBtn = screen.getByLabelText('Close');
    fireEvent.click(closeBtn);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  test('renders modal as a portal directly in document.body when open', async () => {
    mockGetRoundHistory.mockResolvedValueOnce([]);

    const { container } = render(
      <div data-testid="parent-container" style={{ overflow: 'hidden' }}>
        <RoundHistoryModal matchId={1} isOpen={true} onClose={mockOnClose} />
      </div>
    );

    await waitFor(() => {
      expect(screen.getByText('No Completed Rounds Yet')).toBeInTheDocument();
    });

    const modal = screen.getByTestId('round-history-modal');
    // Modal is portaled directly into document.body, outside parent container
    expect(document.body.contains(modal)).toBe(true);
    expect(container.querySelector('[data-testid="round-history-modal"]')).toBeNull();
  });

  test('correctly maps custom team colors and names for Team Emerald and Team Shadow', async () => {
    const mockData: BattleRoundHistoryDto[] = [
      {
        roundId: 201,
        matchId: 101,
        roundNumber: 1,
        winningTeamId: 10,
        targetDamage: 100,
        finalBarState: [
          { teamId: 10, totalDamage: 102 },
          { teamId: 20, totalDamage: 0 },
        ],
        createdAt: '2026-10-12T01:00:00Z',
        endedAt: '2026-10-12T01:05:00Z',
      },
    ];

    mockGetRoundHistory.mockResolvedValueOnce(mockData);

    const customTeamsMap = {
      10: { name: 'Team Emerald', color: '#10B981' },
      20: { name: 'Team Shadow', color: '#A855F7' },
    };

    render(
      <RoundHistoryModal
        matchId={101}
        isOpen={true}
        onClose={mockOnClose}
        teamsMap={customTeamsMap}
        teamAId={10}
        teamBId={20}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Winner: Team Emerald')).toBeInTheDocument();
      expect(screen.getByText('Team Emerald')).toBeInTheDocument();
      expect(screen.getByText('Team Shadow')).toBeInTheDocument();
    });

    const emeraldName = screen.getByText('Team Emerald');
    expect(emeraldName).toHaveStyle({ color: '#10B981' });

    const shadowName = screen.getByText('Team Shadow');
    expect(shadowName).toHaveStyle({ color: '#A855F7' });
  });
});
