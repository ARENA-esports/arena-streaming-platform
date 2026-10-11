import { apiClient } from './client';
import { StreamEngagementResponse } from '../types';

export const analyticsService = {
  /**
   * Fetches viewer engagement analytics summaries per stream.
   * Calls GET /api/analytics/engagement/streams via apiClient.
   */
  getStreamEngagement: async (): Promise<StreamEngagementResponse[]> => {
    const response = await apiClient.get<StreamEngagementResponse[]>('/analytics/engagement/streams');
    return response.data;
  },
};

export default analyticsService;
