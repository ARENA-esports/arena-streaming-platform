import React from 'react';
import { render, screen, act } from '@testing-library/react';
import WalletBalance from '../src/components/common/WalletBalance';
import { useWallet } from '../src/context/WalletContext';

jest.mock('../src/context/WalletContext');

const mockUseWallet = useWallet as jest.MockedFunction<typeof useWallet>;

describe('WalletBalance', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders loading state when context is loading', () => {
    mockUseWallet.mockReturnValue({
      balance: 0,
      isLoading: true,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    render(<WalletBalance />);
    
    // Check if the loading placeholder is present
    expect(screen.getByTitle('Loading balance...')).toBeInTheDocument();
  });

  it('renders the initial balance immediately', () => {
    mockUseWallet.mockReturnValue({
      balance: 1500,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    render(<WalletBalance />);

    expect(screen.getByTitle('Current Coin Balance')).toBeInTheDocument();
    expect(screen.getByText('1,500')).toBeInTheDocument();
  });

  it('animates balance updates smoothly', () => {
    // Initial balance
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    const { rerender } = render(<WalletBalance />);
    expect(screen.getByText('100')).toBeInTheDocument();

    // Update balance to trigger animation
    mockUseWallet.mockReturnValue({
      balance: 500,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    rerender(<WalletBalance />);

    // Initially it should still show 100 before animation progresses
    expect(screen.getByText('100')).toBeInTheDocument();

    // Advance halfway through the 800ms animation
    act(() => {
      jest.advanceTimersByTime(400);
    });

    // The value should be somewhere between 100 and 500
    const textContent = screen.getByTitle('Current Coin Balance').textContent;
    const value = parseInt(textContent?.replace(/,/g, '') || '0', 10);
    expect(value).toBeGreaterThan(100);
    expect(value).toBeLessThan(500);

    // Advance to end of animation
    act(() => {
      jest.advanceTimersByTime(400);
    });

    // Should now show the final value
    expect(screen.getByText('500')).toBeInTheDocument();
  });

  it('handles balance decreases correctly without showing +coins', () => {
    // Initial balance
    mockUseWallet.mockReturnValue({
      balance: 500,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    const { rerender } = render(<WalletBalance />);
    expect(screen.getByText('500')).toBeInTheDocument();

    // Decrease balance
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    rerender(<WalletBalance />);

    // Advance to end of animation
    act(() => {
      jest.advanceTimersByTime(800);
    });

    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.queryByTestId('wallet-reward-badge')).not.toBeInTheDocument();
    expect(screen.queryByText(/\+.*Coins/i)).not.toBeInTheDocument();
  });

  it('initial hydration does not show +coins reward badge', () => {
    // First render during loading
    mockUseWallet.mockReturnValue({
      balance: 0,
      isLoading: true,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    const { rerender } = render(<WalletBalance />);

    // Hydration completes with loaded balance
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    rerender(<WalletBalance />);

    // Advance animation timers
    act(() => {
      jest.advanceTimersByTime(800);
    });

    expect(screen.getByText('100')).toBeInTheDocument();
    // Must NOT display a transient +100 Coins badge
    expect(screen.queryByTestId('wallet-reward-badge')).not.toBeInTheDocument();
    expect(screen.queryByText('+100 Coins')).not.toBeInTheDocument();
  });

  it('shows +X Coins floating badge on watch reward increase and auto-dismisses', () => {
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    const { rerender } = render(<WalletBalance />);
    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.queryByTestId('wallet-reward-badge')).not.toBeInTheDocument();

    // Trigger reward increase (+10 Coins)
    mockUseWallet.mockReturnValue({
      balance: 110,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
      updateBalance: jest.fn(),
      optimisticSpend: jest.fn(),
    });

    rerender(<WalletBalance />);

    // Floating badge must be rendered with +10 Coins
    expect(screen.getByTestId('wallet-reward-badge')).toBeInTheDocument();
    expect(screen.getByText('+10 Coins')).toBeInTheDocument();

    // Auto-dismiss after 1200ms
    act(() => {
      jest.advanceTimersByTime(1200);
    });

    expect(screen.queryByTestId('wallet-reward-badge')).not.toBeInTheDocument();
    expect(screen.queryByText('+10 Coins')).not.toBeInTheDocument();
  });
});
