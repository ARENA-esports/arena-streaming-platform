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
});
