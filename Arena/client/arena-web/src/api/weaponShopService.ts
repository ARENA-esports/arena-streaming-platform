import { apiClient } from './client';
import { Weapon, AttackRequest, AttackResponse, BattleRoundHistoryDto } from '../types';

export const weaponShopService = {
  /**
   * Fetches active weapons available for purchase in the weapon shop.
   * Calls GET /api/economy/weapons.
   */
  getWeapons: async (): Promise<Weapon[]> => {
    const response = await apiClient.get<Weapon[]>('/economy/weapons');
    return response.data;
  },

  /**
   * Purchases a weapon attack for the viewer's chosen team in an ongoing match.
   * Calls POST /api/economy/attack.
   */
  purchaseAttack: async (data: AttackRequest): Promise<AttackResponse> => {
    const response = await apiClient.post<AttackResponse>('/economy/attack', data);
    return response.data;
  },

  /**
   * Retrieves past completed round outcome history for a match (SCRUM-123).
   * Calls GET /api/economy/rounds/{matchId}/history.
   */
  getRoundHistory: async (matchId: number): Promise<BattleRoundHistoryDto[]> => {
    const response = await apiClient.get<BattleRoundHistoryDto[]>(`/economy/rounds/${matchId}/history`);
    return response.data;
  },
};
