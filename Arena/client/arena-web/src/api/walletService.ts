import { apiClient } from './client';
import { WalletBalanceResponse } from '../types';

export const walletService = {
  /**
   * Fetches the current coin balance for the authenticated viewer.
   * Calls GET /api/wallet/balance.
   */
  getBalance: async (): Promise<WalletBalanceResponse> => {
    const response = await apiClient.get<WalletBalanceResponse>('/wallet/balance');
    return response.data;
  },
};
