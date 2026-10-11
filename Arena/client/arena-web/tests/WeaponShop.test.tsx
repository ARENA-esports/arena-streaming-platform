import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import WeaponShop from '../src/components/match/WeaponShop';
import { weaponShopService } from '../src/api/weaponShopService';
import { useWallet } from '../src/context/WalletContext';
import { useNotification } from '../src/context/NotificationContext';
import { useAuth } from '../src/context/AuthContext';
import { Weapon } from '../src/types';

jest.mock('../src/api/weaponShopService');
jest.mock('../src/context/WalletContext');
jest.mock('../src/context/NotificationContext');
jest.mock('../src/context/AuthContext');

const mockUseWallet = useWallet as jest.MockedFunction<typeof useWallet>;
const mockUseNotification = useNotification as jest.MockedFunction<typeof useNotification>;
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

const sampleWeapons: Weapon[] = [
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
    cost: 50,
    damage: 6,
    iconKey: 'crossbow',
  },
];

describe('WeaponShop Component', () => {
  const mockNotify = jest.fn();
  const mockUpdateBalance = jest.fn();
  const mockOptimisticSpend = jest.fn();
  const mockRollback = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();

    mockUseAuth.mockReturnValue({
      user: { userId: 1, username: 'TestViewer', email: 'test@arena.com', role: 'Viewer' },
      token: 'valid-token',
      login: jest.fn(),
      signup: jest.fn(),
      logout: jest.fn(),
      refreshAuth: jest.fn(),
      isLoading: false,
    });

    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
      optimisticSpend: mockOptimisticSpend.mockReturnValue({
        success: true,
        rollback: mockRollback,
      }),
    });

    mockUseNotification.mockReturnValue({
      notifications: [],
      notify: mockNotify,
      dismiss: jest.fn(),
    });

    (weaponShopService.getWeapons as jest.Mock).mockResolvedValue(sampleWeapons);
  });

  it('renders loading state initially while fetching weapons', () => {
    (weaponShopService.getWeapons as jest.Mock).mockReturnValue(new Promise(() => {}));

    render(<WeaponShop matchId={10} teamId={1} />);

    expect(screen.getByTestId('weapon-shop-loading')).toBeInTheDocument();
    expect(screen.getByText('Loading Armory...')).toBeInTheDocument();
  });

  it('renders weapon cards from API once loaded', async () => {
    render(<WeaponShop matchId={10} teamId={1} />);

    await waitFor(() => {
      expect(screen.getByText('Throwing Knife')).toBeInTheDocument();
      expect(screen.getByText('Crossbow Bolt')).toBeInTheDocument();
    });

    expect(screen.getByText('A quick, cheap strike.')).toBeInTheDocument();
    expect(screen.getByText('+1 DMG')).toBeInTheDocument();
    expect(screen.getByText('+6 DMG')).toBeInTheDocument();
  });

  it('shows warning and disables attack buttons when no team is selected (teamId=null)', async () => {
    render(<WeaponShop matchId={10} teamId={null} />);

    await waitFor(() => {
      expect(screen.getByText('Throwing Knife')).toBeInTheDocument();
    });

    expect(screen.getByTestId('no-team-warning')).toBeInTheDocument();
    expect(
      screen.getByText(/Select a faction team above to unlock weapon attacks/i)
    ).toBeInTheDocument();

    const buyButton1 = screen.getByTestId('buy-weapon-1');
    const buyButton2 = screen.getByTestId('buy-weapon-2');

    expect(buyButton1).toBeDisabled();
    expect(buyButton2).toBeDisabled();
  });

  it('disables weapon button when user balance is insufficient', async () => {
    // Balance is 20: Knife costs 10 (affordable), Crossbow costs 50 (unaffordable)
    mockUseWallet.mockReturnValue({
      balance: 20,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: mockUpdateBalance,
      optimisticSpend: mockOptimisticSpend,
    });

    render(<WeaponShop matchId={10} teamId={1} />);

    await waitFor(() => {
      expect(screen.getByText('Throwing Knife')).toBeInTheDocument();
    });

    const knifeButton = screen.getByTestId('buy-weapon-1');
    const crossbowButton = screen.getByTestId('buy-weapon-2');

    expect(knifeButton).not.toBeDisabled();
    expect(crossbowButton).toBeDisabled();
    expect(crossbowButton).toHaveAttribute('title', 'Not enough coins');
  });

  it('executes successful attack: optimistic deduction, API call, balance update and success notification', async () => {
    (weaponShopService.purchaseAttack as jest.Mock).mockResolvedValueOnce({
      success: true,
      attackId: 777,
      coinsSpent: 10,
      currentBalance: 90,
      damageDealt: 1,
      message: 'Throwing Knife attack launched! Dealt 1 damage.',
    });

    render(<WeaponShop matchId={10} teamId={2} />);

    await waitFor(() => {
      expect(screen.getByText('Throwing Knife')).toBeInTheDocument();
    });

    const buyButton = screen.getByTestId('buy-weapon-1');
    fireEvent.click(buyButton);

    expect(mockOptimisticSpend).toHaveBeenCalledWith(10);

    await waitFor(() => {
      expect(weaponShopService.purchaseAttack).toHaveBeenCalledWith({
        weaponId: 1,
        matchId: 10,
        teamId: 2,
      });
      expect(mockUpdateBalance).toHaveBeenCalledWith(90);
      expect(mockNotify).toHaveBeenCalledWith(
        'Throwing Knife attack launched! Dealt 1 damage.',
        'success'
      );
    });
  });

  it('rolls back balance and shows error toast when purchaseAttack fails', async () => {
    (weaponShopService.purchaseAttack as jest.Mock).mockRejectedValueOnce({
      response: {
        data: {
          message: 'Insufficient coins on server.',
        },
      },
    });

    render(<WeaponShop matchId={10} teamId={2} />);

    await waitFor(() => {
      expect(screen.getByText('Throwing Knife')).toBeInTheDocument();
    });

    const buyButton = screen.getByTestId('buy-weapon-1');
    fireEvent.click(buyButton);

    expect(mockOptimisticSpend).toHaveBeenCalledWith(10);

    await waitFor(() => {
      expect(mockRollback).toHaveBeenCalled();
      expect(mockNotify).toHaveBeenCalledWith('Insufficient coins on server.', 'error');
    });
  });

  it('renders error state when weapon catalog fails to load and allows retry', async () => {
    (weaponShopService.getWeapons as jest.Mock).mockRejectedValueOnce(
      new Error('Network error')
    );

    render(<WeaponShop matchId={10} teamId={1} />);

    await waitFor(() => {
      expect(
        screen.getByText('Unable to load weapons. Please try again.')
      ).toBeInTheDocument();
    });

    // Mock success on retry
    (weaponShopService.getWeapons as jest.Mock).mockResolvedValueOnce(sampleWeapons);

    const retryBtn = screen.getByText('Retry');
    fireEvent.click(retryBtn);

    await waitFor(() => {
      expect(screen.getByText('Throwing Knife')).toBeInTheDocument();
    });
  });
});
