import { apiClient } from './client';
import { WatchTickRequest, WatchTickResponse } from '../types';

export const economyService = {
  /**
   * Records a watch tick for the authenticated viewer to earn coins.
   * Calls POST /api/economy/watch-tick with optional streamId.
   */
  recordWatchTick: async (data?: WatchTickRequest): Promise<WatchTickResponse> => {
    const response = await apiClient.post<WatchTickResponse>('/economy/watch-tick', data);
    return response.data;
  },

  /**
   * Alias for recordWatchTick to support flexible naming conventions.
   */
  watchTick: async (data?: WatchTickRequest): Promise<WatchTickResponse> => {
    const response = await apiClient.post<WatchTickResponse>('/economy/watch-tick', data);
    return response.data;
  },
};
