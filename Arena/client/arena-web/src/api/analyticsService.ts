import { apiClient } from './client';
import {
  StreamEngagementResponse,
  StreamTeamBattleResponse,
  StreamRoundOutcomeResponse,
  StreamBattleDashboardResponse,
} from '../types';

export const analyticsService = {
  /**
   * Fetches viewer engagement analytics summaries per stream (SCRUM-125).
   * Calls GET /api/analytics/engagement/streams via apiClient.
   */
  getStreamEngagement: async (): Promise<StreamEngagementResponse[]> => {
    const response = await apiClient.get<StreamEngagementResponse[]>('/analytics/engagement/streams');
    return response.data;
  },

  /**
   * Fetches aggregated battle and attack statistics across all streams and teams (SCRUM-126).
   * Calls GET /api/analytics/battle/streams via apiClient.
   */
  getAllBattleSummaries: async (): Promise<StreamTeamBattleResponse[]> => {
    const response = await apiClient.get<StreamTeamBattleResponse[]>('/analytics/battle/streams');
    return response.data;
  },

  /**
   * Fetches comprehensive battle dashboard data (team summaries and round outcomes) for a specific stream (SCRUM-126).
   * Calls GET /api/analytics/battle/streams/{streamId} via apiClient.
   */
  getStreamBattleDashboard: async (streamId: number): Promise<StreamBattleDashboardResponse> => {
    const response = await apiClient.get<StreamBattleDashboardResponse>(`/analytics/battle/streams/${streamId}`);
    return response.data;
  },

  /**
   * Fetches sequential round outcomes for a specific stream (SCRUM-126).
   * Calls GET /api/analytics/battle/streams/{streamId}/rounds via apiClient.
   */
  getRoundOutcomes: async (streamId: number): Promise<StreamRoundOutcomeResponse[]> => {
    const response = await apiClient.get<StreamRoundOutcomeResponse[]>(`/analytics/battle/streams/${streamId}/rounds`);
    return response.data;
  },
};

export default analyticsService;
