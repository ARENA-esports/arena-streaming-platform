import React, { createContext, useContext, useState, useEffect } from 'react';
import { UserProfile, LoginRequest, LoginResponse } from '../types';
import { authService } from '../api/authService';

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  completeTwitchLogin: (code: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('arena_access_token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const refreshProfile = async () => {
    try {
      try {
        const refreshRes = await authService.refresh();
        if (refreshRes?.token) {
          localStorage.setItem('arena_access_token', refreshRes.token);
          setToken(refreshRes.token);
        }
      } catch {
        // Continue to getMe if refresh cookie/header is unavailable
      }

      const profile = await authService.getMe();
      setUser(profile);
    } catch (error) {
      setUser(null);
      setToken(null);
      localStorage.removeItem('arena_access_token');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (token) {
      refreshProfile();
    } else {
      setIsLoading(false);
    }
  }, [token]);

  const login = async (data: LoginRequest) => {
    const res: LoginResponse = await authService.login(data);
    localStorage.setItem('arena_access_token', res.token);
    setToken(res.token);
    setUser({
      userId: res.userId,
      username: res.username,
      email: res.email,
      role: res.role,
    });
  };

  const completeTwitchLogin = async (code: string) => {
    const res = await authService.exchangeTwitchCode(code);
    localStorage.setItem('arena_access_token', res.token);
    setToken(res.token);
    setUser({
      userId: res.userId,
      username: res.username,
      email: res.email,
      role: res.role,
    });
  };

  const logout = async () => {
    try {
      if (token) await authService.logout();
    } catch (e) {
      console.error('Logout failed on server', e);
    } finally {
      localStorage.removeItem('arena_access_token');
      setToken(null);
      setUser(null);
      window.location.href = '/login';
    }
  };

  return (
    <AuthContext.Provider value={{ user, token, isLoading, login, completeTwitchLogin, logout, refreshProfile }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
