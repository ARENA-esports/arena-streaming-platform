import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { WalletProvider, useWallet } from '../src/context/WalletContext';
import { walletService } from '../src/api/walletService';
import { useAuth } from '../src/context/AuthContext';

jest.mock('../src/api/walletService');
jest.mock('../src/context/AuthContext');

const mockWalletService = walletService as jest.Mocked<typeof walletService>;
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

const TestComponent = () => {
  const { balance, isLoading, error } = useWallet();
  return (
    <div>
      <div data-testid="loading">{isLoading.toString()}</div>
      <div data-testid="balance">{balance}</div>
      <div data-testid="error">{error ? error.message : 'no-error'}</div>
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
});
