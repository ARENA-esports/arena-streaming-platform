import React from 'react';
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react';
import { WalletProvider, useWallet } from '../src/context/WalletContext';
import { walletService } from '../src/api/walletService';
import { useAuth } from '../src/context/AuthContext';

jest.mock('../src/api/walletService');
jest.mock('../src/context/AuthContext');

const mockWalletService = walletService as jest.Mocked<typeof walletService>;
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

const TestComponent = () => {
  const { balance, isLoading, error, updateBalance, optimisticSpend } = useWallet();
  const [lastSpendResult, setLastSpendResult] = React.useState<{success: boolean, rollback: () => void} | null>(null);

  return (
    <div>
      <div data-testid="loading">{isLoading.toString()}</div>
      <div data-testid="balance">{balance}</div>
      <div data-testid="error">{error ? error.message : 'no-error'}</div>
      <button data-testid="spend-btn" onClick={() => setLastSpendResult(optimisticSpend(25))}>Spend 25</button>
      <button data-testid="rollback-btn" onClick={() => lastSpendResult?.rollback()}>Rollback</button>
      <div data-testid="spend-success">{lastSpendResult ? lastSpendResult.success.toString() : 'none'}</div>
      <button data-testid="update-btn" onClick={() => updateBalance(200)}>Update 200</button>
    </div>
  );
};

describe('WalletContext', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('does not fetch balance if user is not authenticated', async () => {
    mockUseAuth.mockReturnValue({
      user: null,
      token: null,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    render(
      <WalletProvider>
        <TestComponent />
      </WalletProvider>
    );

    expect(mockWalletService.getBalance).not.toHaveBeenCalled();
    expect(screen.getByTestId('loading')).toHaveTextContent('false');
    expect(screen.getByTestId('balance')).toHaveTextContent('0');
  });

  it('fetches balance and hydrates state when authenticated', async () => {
    mockUseAuth.mockReturnValue({
      user: { userId: 1, username: 'test', email: 'test@test.com', role: 'Viewer' },
      token: 'fake-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockWalletService.getBalance.mockResolvedValueOnce({ balance: 150 });

    render(
      <WalletProvider>
        <TestComponent />
      </WalletProvider>
    );

    expect(screen.getByTestId('loading')).toHaveTextContent('true');

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('false');
    });

    expect(mockWalletService.getBalance).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('balance')).toHaveTextContent('150');
    expect(screen.getByTestId('error')).toHaveTextContent('no-error');
  });

  it('sets error state when balance fetch fails', async () => {
    mockUseAuth.mockReturnValue({
      user: { userId: 1, username: 'test', email: 'test@test.com', role: 'Viewer' },
      token: 'fake-token',
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
      refreshProfile: jest.fn(),
    });

    mockWalletService.getBalance.mockRejectedValueOnce(new Error('Network error'));

    // Prevent console.error from cluttering the test output
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <WalletProvider>
        <TestComponent />
      </WalletProvider>
    );

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('false');
    });

    expect(screen.getByTestId('error')).toHaveTextContent('Network error');
    expect(screen.getByTestId('balance')).toHaveTextContent('0');
    
    consoleSpy.mockRestore();
  });

  describe('optimisticSpend and updateBalance', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: { userId: 1, username: 'test', email: 'test@test.com', role: 'Viewer' },
        token: 'fake-token',
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshProfile: jest.fn(),
      });
    });

    it('handles successful optimistic spend', async () => {
      mockWalletService.getBalance.mockResolvedValueOnce({ balance: 100 });

      render(
        <WalletProvider>
          <TestComponent />
        </WalletProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('false');
      });

      expect(screen.getByTestId('balance')).toHaveTextContent('100');

      // Attempt spend 25
      act(() => {
        fireEvent.click(screen.getByTestId('spend-btn'));
      });

      // Immediate reduction
      expect(screen.getByTestId('balance')).toHaveTextContent('75');
      expect(screen.getByTestId('spend-success')).toHaveTextContent('true');
    });

    it('rejects spend if insufficient balance', async () => {
      mockWalletService.getBalance.mockResolvedValueOnce({ balance: 20 });

      render(
        <WalletProvider>
          <TestComponent />
        </WalletProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('false');
      });

      expect(screen.getByTestId('balance')).toHaveTextContent('20');

      // Attempt spend 25
      act(() => {
        fireEvent.click(screen.getByTestId('spend-btn'));
      });

      // Balance remains 20
      expect(screen.getByTestId('balance')).toHaveTextContent('20');
      expect(screen.getByTestId('spend-success')).toHaveTextContent('false');
    });

    it('allows rollback of optimistic spend', async () => {
      mockWalletService.getBalance.mockResolvedValueOnce({ balance: 100 });

      render(
        <WalletProvider>
          <TestComponent />
        </WalletProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('false');
      });

      // Attempt spend 25
      act(() => {
        fireEvent.click(screen.getByTestId('spend-btn'));
      });
      expect(screen.getByTestId('balance')).toHaveTextContent('75');

      // Trigger rollback
      act(() => {
        fireEvent.click(screen.getByTestId('rollback-btn'));
      });

      // Restored
      expect(screen.getByTestId('balance')).toHaveTextContent('100');
    });

    it('updates balance via updateBalance', async () => {
      mockWalletService.getBalance.mockResolvedValueOnce({ balance: 100 });

      render(
        <WalletProvider>
          <TestComponent />
        </WalletProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('false');
      });

      act(() => {
        fireEvent.click(screen.getByTestId('update-btn'));
      });
      expect(screen.getByTestId('balance')).toHaveTextContent('200');
    });
  });
});
