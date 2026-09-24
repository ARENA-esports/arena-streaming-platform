import { economyService } from '../src/api/economyService';
import { apiClient } from '../src/api/client';
import { WatchTickResponse } from '../src/types';

jest.mock('../src/api/client', () => ({
  apiClient: {
    post: jest.fn(),
  },
}));

describe('economyService', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  test('recordWatchTick sends POST request to /economy/watch-tick with streamId', async () => {
    const mockResponse: WatchTickResponse = {
      success: true,
      coinsAwarded: 10,
      currentBalance: 150,
      lastTickAt: '2026-09-25T01:00:00Z',
      remainingSeconds: 60,
      message: 'Watch tick recorded successfully',
    };

    (apiClient.post as jest.Mock).mockResolvedValueOnce({ data: mockResponse });

    const result = await economyService.recordWatchTick({ streamId: 101 });

    expect(apiClient.post).toHaveBeenCalledWith('/economy/watch-tick', { streamId: 101 });
    expect(result).toEqual(mockResponse);
  });

  test('recordWatchTick works without streamId parameter', async () => {
    const mockResponse: WatchTickResponse = {
      success: true,
      coinsAwarded: 10,
      currentBalance: 160,
      lastTickAt: '2026-09-25T01:01:00Z',
      message: 'Watch tick recorded successfully',
    };

    (apiClient.post as jest.Mock).mockResolvedValueOnce({ data: mockResponse });

    const result = await economyService.recordWatchTick();

    expect(apiClient.post).toHaveBeenCalledWith('/economy/watch-tick', undefined);
    expect(result).toEqual(mockResponse);
  });

  test('watchTick alias invokes same endpoint correctly', async () => {
    const mockResponse: WatchTickResponse = {
      success: true,
      coinsAwarded: 10,
      currentBalance: 170,
      lastTickAt: '2026-09-25T01:02:00Z',
      message: 'Watch tick recorded successfully',
    };

    (apiClient.post as jest.Mock).mockResolvedValueOnce({ data: mockResponse });

    const result = await economyService.watchTick({ streamId: 202 });

    expect(apiClient.post).toHaveBeenCalledWith('/economy/watch-tick', { streamId: 202 });
    expect(result).toEqual(mockResponse);
  });
});
