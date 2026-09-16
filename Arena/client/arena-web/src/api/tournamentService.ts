import { apiClient } from './client';
import {
  TournamentResponse,
  CreateTournamentRequest,
  UpdateTournamentRequest
} from '../types';

export const tournamentService = {
  createTournament: async (data: CreateTournamentRequest) => {
    const response = await apiClient.post<TournamentResponse>('/tournaments', data);
    return response.data;
  },
  getTournament: async (id: number) => {
    const response = await apiClient.get<TournamentResponse>(`/tournaments/${id}`);
    return response.data;
  },
  getAllTournaments: async (status?: string) => {
    const response = await apiClient.get<TournamentResponse[]>('/tournaments', {
      params: { status }
    });
    return response.data;
  },
  updateTournament: async (id: number, data: UpdateTournamentRequest) => {
    const response = await apiClient.put<TournamentResponse>(`/tournaments/${id}`, data);
    return response.data;
  },
  cancelTournament: async (id: number) => {
    const response = await apiClient.patch<TournamentResponse>(`/tournaments/${id}/cancel`);
    return response.data;
  }
};
