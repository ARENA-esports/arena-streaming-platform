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
    });

    const { rerender } = render(<WalletBalance />);
    expect(screen.getByText('100')).toBeInTheDocument();

    // Update balance to trigger animation
    mockUseWallet.mockReturnValue({
      balance: 500,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
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

  it('handles balance decreases correctly', () => {
    // Initial balance
    mockUseWallet.mockReturnValue({
      balance: 500,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
    });

    const { rerender } = render(<WalletBalance />);
    expect(screen.getByText('500')).toBeInTheDocument();

    // Decrease balance
    mockUseWallet.mockReturnValue({
      balance: 100,
      isLoading: false,
      error: null,
      setBalance: jest.fn(),
    });

    rerender(<WalletBalance />);

    // Advance to end of animation
    act(() => {
      jest.advanceTimersByTime(800);
    });

    expect(screen.getByText('100')).toBeInTheDocument();
  });
});
