import { analyticsService } from '../src/api/analyticsService';
import { apiClient } from '../src/api/client';
import { StreamEngagementResponse } from '../src/types';

jest.mock('../src/api/client', () => ({
  apiClient: {
    get: jest.fn(),
  },
}));

describe('analyticsService', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  test('getStreamEngagement sends GET request to /analytics/engagement/streams', async () => {
    const mockData: StreamEngagementResponse[] = [
      {
        streamId: 101,
        totalWatchSeconds: 7200,
        totalCoinsEarned: 1200,
        totalWatchTicks: 120,
        uniqueViewers: 25,
        lastEventAt: '2026-10-05T10:30:00Z',
      },
    ];

    (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: mockData });

    const result = await analyticsService.getStreamEngagement();

    expect(apiClient.get).toHaveBeenCalledTimes(1);
    expect(apiClient.get).toHaveBeenCalledWith('/analytics/engagement/streams');
    expect(result).toEqual(mockData);
  });

  test('getStreamEngagement returns empty array when backend has no records', async () => {
    (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: [] });

    const result = await analyticsService.getStreamEngagement();

    expect(apiClient.get).toHaveBeenCalledWith('/analytics/engagement/streams');
    expect(result).toEqual([]);
  });

  test('getStreamEngagement throws when apiClient rejects', async () => {
    (apiClient.get as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

    await expect(analyticsService.getStreamEngagement()).rejects.toThrow('Network error');
  });

  describe('Battle Stats API Methods', () => {
    test('getAllBattleSummaries sends GET request to /analytics/battle/streams', async () => {
      const mockSummaries = [
        {
          streamId: 201,
          teamId: 'team-a',
          teamName: 'Cyber Hawks',
          totalAttacks: 150,
          totalDamageDealt: 4500,
          totalCoinsSpent: 800,
          roundsWon: 3,
          roundsLost: 1,
          lastEventAt: '2026-10-06T10:00:00Z',
        },
      ];

      (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: mockSummaries });

      const result = await analyticsService.getAllBattleSummaries();

      expect(apiClient.get).toHaveBeenCalledTimes(1);
      expect(apiClient.get).toHaveBeenCalledWith('/analytics/battle/streams');
      expect(result).toEqual(mockSummaries);
    });

    test('getAllBattleSummaries returns empty array when backend has no records', async () => {
      (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: [] });

      const result = await analyticsService.getAllBattleSummaries();

      expect(apiClient.get).toHaveBeenCalledWith('/analytics/battle/streams');
      expect(result).toEqual([]);
    });

    test('getStreamBattleDashboard sends GET request to /analytics/battle/streams/:streamId', async () => {
      const mockDashboard = {
        streamId: 201,
        teams: [
          {
            streamId: 201,
            teamId: 'team-a',
            teamName: 'Cyber Hawks',
            totalAttacks: 150,
            totalDamageDealt: 4500,
            totalCoinsSpent: 800,
            roundsWon: 3,
            roundsLost: 1,
            lastEventAt: '2026-10-06T10:00:00Z',
          },
          {
            streamId: 201,
            teamId: 'team-b',
            teamName: 'Shadow Tigers',
            totalAttacks: 120,
            totalDamageDealt: 3800,
            totalCoinsSpent: 600,
            roundsWon: 1,
            roundsLost: 3,
            lastEventAt: '2026-10-06T10:05:00Z',
          },
        ],
        rounds: [
          {
            streamId: 201,
            roundNumber: 1,
            winningTeamId: 'team-a',
            winningTeamName: 'Cyber Hawks',
            teamAId: 'team-a',
            teamBId: 'team-b',
            teamAAttacks: 35,
            teamBAttacks: 28,
            teamADamage: 1100,
            teamBDamage: 950,
            completedAt: '2026-10-06T09:45:00Z',
          },
        ],
      };

      (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: mockDashboard });

      const result = await analyticsService.getStreamBattleDashboard(201);

      expect(apiClient.get).toHaveBeenCalledTimes(1);
      expect(apiClient.get).toHaveBeenCalledWith('/analytics/battle/streams/201');
      expect(result).toEqual(mockDashboard);
    });

    test('getStreamBattleDashboard throws when apiClient rejects', async () => {
      (apiClient.get as jest.Mock).mockRejectedValueOnce(new Error('Dashboard error'));

      await expect(analyticsService.getStreamBattleDashboard(999)).rejects.toThrow('Dashboard error');
    });

    test('getRoundOutcomes sends GET request to /analytics/battle/streams/:streamId/rounds', async () => {
      const mockRounds = [
        {
          streamId: 201,
          roundNumber: 1,
          winningTeamId: 'team-a',
          winningTeamName: 'Cyber Hawks',
          teamAId: 'team-a',
          teamBId: 'team-b',
          teamAAttacks: 35,
          teamBAttacks: 28,
          teamADamage: 1100,
          teamBDamage: 950,
          completedAt: '2026-10-06T09:45:00Z',
        },
      ];

      (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: mockRounds });

      const result = await analyticsService.getRoundOutcomes(201);

      expect(apiClient.get).toHaveBeenCalledTimes(1);
      expect(apiClient.get).toHaveBeenCalledWith('/analytics/battle/streams/201/rounds');
      expect(result).toEqual(mockRounds);
    });

    test('getRoundOutcomes throws when apiClient rejects', async () => {
      (apiClient.get as jest.Mock).mockRejectedValueOnce(new Error('Round fetch error'));

      await expect(analyticsService.getRoundOutcomes(201)).rejects.toThrow('Round fetch error');
    });
  });
});
