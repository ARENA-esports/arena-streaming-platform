import { apiClient } from './client';
import { Weapon, AttackRequest, AttackResponse } from '../types';

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
};
