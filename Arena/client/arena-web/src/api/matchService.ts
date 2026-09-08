import { apiClient } from './client';
import {
  CreateMatchRequest,
  MatchResponse,
  LinkStreamRequest,
  StreamResponse,
  UpdateMatchRequest,
  UpdateMatchStatusRequest
} from '../types';

export const matchService = {
  createMatch: async (data: CreateMatchRequest) => {
    const response = await apiClient.post<MatchResponse>('/matches', data);
    return response.data;
  },
  getMatch: async (id: number) => {
    const response = await apiClient.get<MatchResponse>(`/matches/${id}`);
    return response.data;
  },
  linkStream: async (matchId: number, data: LinkStreamRequest) => {
    const response = await apiClient.post<StreamResponse>(`/matches/${matchId}/streams`, data);
    return response.data;
  },
  getMatchStream: async (matchId: number) => {
    // For Twitch iframes, we can pass the current host domain as parent
    const response = await apiClient.get<StreamResponse>(`/matches/${matchId}/stream`, {
      params: { parentDomain: window.location.hostname }
    });
    return response.data;
  },
  updateMatch: async (id: number, data: UpdateMatchRequest) => {
    const response = await apiClient.put<MatchResponse>(`/matches/${id}`, data);
    return response.data;
  },
  updateMatchStatus: async (id: number, data: UpdateMatchStatusRequest) => {
    const response = await apiClient.patch<MatchResponse>(`/matches/${id}/status`, data);
    return response.data;
  },
  deleteMatch: async (id: number) => {
    await apiClient.delete(`/matches/${id}`);
  }
};
