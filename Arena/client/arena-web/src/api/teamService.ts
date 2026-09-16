import { apiClient } from './client';

export interface TeamMember {
  playerId: number;
  teamId: number;
  username: string;
  role?: string;
  isActive: boolean;
}

export interface Team {
  teamId: number;
  name: string;
  colorHex: string;
  logoUrl?: string;
  players?: TeamMember[];
  roster?: TeamMember[];
}

export interface CreateTeamRequest {
  teamName: string;
  colorHex: string;
}

export interface UpdateTeamRequest {
  teamName?: string;
  colorHex?: string;
  logoUrl?: string;
}

export interface AddPlayerRequest {
  username?: string;
  role?: string;
  name?: string;
  position?: string;
}

const normalizePlayer = (raw: any): TeamMember => ({
  playerId: raw?.player_id ?? raw?.playerId ?? raw?.id ?? 0,
  teamId: raw?.team_id ?? raw?.teamId ?? 0,
  username: raw?.username ?? raw?.player_name ?? raw?.name ?? 'Player',
  role: raw?.role ?? raw?.position ?? 'Player',
  isActive: raw?.is_active ?? raw?.isActive ?? true
});

export const formatLogoUrl = (url?: string | null): string => {
  if (!url || typeof url !== 'string') return '';
  let trimmed = url.trim();
  if (trimmed.startsWith('http://localhost/uploads/')) {
    trimmed = trimmed.replace('http://localhost/uploads/', '/uploads/');
  } else if (trimmed.startsWith('http://127.0.0.1:8082/uploads/')) {
    trimmed = trimmed.replace('http://127.0.0.1:8082/uploads/', '/uploads/');
  } else if (trimmed.startsWith('http://localhost:8082/uploads/')) {
    trimmed = trimmed.replace('http://localhost:8082/uploads/', '/uploads/');
  }
  return trimmed;
};

const normalizeTeam = (raw: any): Team => {
  const rawPlayers = raw?.players || raw?.roster || [];
  const rawLogo = raw?.logo_url ?? raw?.logoUrl ?? undefined;
  return {
    teamId: raw?.team_id ?? raw?.teamId ?? raw?.id ?? 0,
    name: raw?.team_name ?? raw?.teamName ?? raw?.name ?? 'Unnamed Team',
    colorHex: raw?.color_hex ?? raw?.colorHex ?? '#00B8FC',
    logoUrl: rawLogo ? formatLogoUrl(rawLogo) : undefined,
    players: Array.isArray(rawPlayers) ? rawPlayers.map(normalizePlayer) : []
  };
};

export const teamService = {
  async getAllTeams(): Promise<Team[]> {
    const response = await apiClient.get<any[]>('/teams');
    const data = Array.isArray(response.data) ? response.data : [];
    return data.map(normalizeTeam);
  },

  async getTeamById(id: number): Promise<Team> {
    const response = await apiClient.get<any>(`/teams/${id}`);
    return normalizeTeam(response.data);
  },

  async createTeam(data: CreateTeamRequest): Promise<Team> {
    const response = await apiClient.post<Team>('/teams', {
      team_name: data.teamName,
      color_hex: data.colorHex
    });
    return response.data;
  },

  async updateTeam(id: number, data: UpdateTeamRequest): Promise<Team> {
    const payload: Record<string, string> = {};
    if (data.teamName) payload.team_name = data.teamName;
    if (data.colorHex) payload.color_hex = data.colorHex;
    if (data.logoUrl !== undefined) payload.logo_url = data.logoUrl;
    
    const response = await apiClient.put<Team>(`/teams/${id}`, payload);
    return response.data;
  },

  async addPlayer(id: number, data: AddPlayerRequest): Promise<TeamMember> {
    const username = data.username || data.name || '';
    const role = data.role || data.position || 'Player';
    const response = await apiClient.post<TeamMember>(`/teams/${id}/players`, {
      username,
      role
    });
    return response.data;
  },

  async removePlayer(teamId: number, playerId: number): Promise<void> {
    await apiClient.delete(`/teams/${teamId}/players/${playerId}`);
  },

  async uploadLogo(teamId: number, file: File): Promise<{ logoUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post<any>(`/teams/${teamId}/logo`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    const url = response.data?.logo_url || response.data?.logoUrl || '';
    return { logoUrl: url };
  }
};
