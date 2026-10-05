import { weaponShopService } from '../src/api/weaponShopService';
import { apiClient } from '../src/api/client';
import { Weapon, AttackResponse } from '../src/types';

jest.mock('../src/api/client', () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
  },
}));

describe('weaponShopService', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  test('getWeapons sends GET request to /economy/weapons and returns weapon catalog', async () => {
    const mockWeapons: Weapon[] = [
      {
        weaponId: 1,
        name: 'Throwing Knife',
        description: 'A quick, cheap strike.',
        cost: 10,
        damage: 1,
        iconKey: 'knife',
      },
      {
        weaponId: 2,
        name: 'Crossbow Bolt',
        description: 'Solid ranged damage.',
        cost: 25,
        damage: 3,
        iconKey: 'crossbow',
      },
    ];

    (apiClient.get as jest.Mock).mockResolvedValueOnce({ data: mockWeapons });

    const result = await weaponShopService.getWeapons();

    expect(apiClient.get).toHaveBeenCalledWith('/economy/weapons');
    expect(result).toEqual(mockWeapons);
  });

  test('purchaseAttack sends POST request to /economy/attack with payload', async () => {
    const payload = { weaponId: 1, matchId: 10, teamId: 2 };
    const mockResponse: AttackResponse = {
      success: true,
      attackId: 101,
      coinsSpent: 10,
      currentBalance: 90,
      damageDealt: 1,
      message: 'Throwing Knife attack launched! Dealt 1 damage.',
    };

    (apiClient.post as jest.Mock).mockResolvedValueOnce({ data: mockResponse });

    const result = await weaponShopService.purchaseAttack(payload);

    expect(apiClient.post).toHaveBeenCalledWith('/economy/attack', payload);
    expect(result).toEqual(mockResponse);
  });
});
