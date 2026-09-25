import React, { createContext, useContext, useState, useEffect } from 'react';
import { walletService } from '../api/walletService';
import { useAuth } from './AuthContext';

interface WalletContextType {
  balance: number;
  isLoading: boolean;
  error: Error | null;
  setBalance: React.Dispatch<React.SetStateAction<number>>;
  updateBalance: (newBalance: number) => void;
}

const WalletContext = createContext<WalletContextType | undefined>(undefined);

export const WalletProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, token } = useAuth();
  const [balance, setBalance] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    // Only hydrate if user is authenticated
    if (!token || !user) {
      setBalance(0);
      setIsLoading(false);
      return;
    }

    const fetchBalance = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await walletService.getBalance();
        setBalance(data.balance);
      } catch (err) {
        const fetchError = err instanceof Error ? err : new Error('Failed to fetch balance');
        setError(fetchError);
        console.error('Wallet hydration failed:', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchBalance();
  }, [token, user]);

  const updateBalance = (newBalance: number) => {
    setBalance(newBalance);
  };

  return (
    <WalletContext.Provider value={{ balance, isLoading, error, setBalance, updateBalance }}>
      {children}
    </WalletContext.Provider>
  );
};

export const useWallet = () => {
  const context = useContext(WalletContext);
  if (context === undefined) {
    throw new Error('useWallet must be used within a WalletProvider');
  }
  return context;
};
