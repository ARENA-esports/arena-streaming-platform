import React, { createContext, useContext, useState, useEffect } from 'react';
import { UserProfile, LoginRequest, LoginResponse } from '../types';
import { authService } from '../api/authService';

export const getJwtRole = (jwtToken: string): string | null => {
  try {
    const parts = jwtToken.split('.');
    if (parts.length < 2) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    const payload = JSON.parse(jsonPayload);
    return (
      payload.role ||
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      null
    );
  } catch {
    return null;
  }
};

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  completeTwitchLogin: (code: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
  setSessionToken: (newToken: string) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('arena_access_token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const setSessionToken = (newToken: string) => {
    localStorage.setItem('arena_access_token', newToken);
    setToken(newToken);
  };

  const refreshProfile = async () => {
    try {
      const profile = await authService.getMe();
      setUser(profile);

      // Verify if token role matches database profile role.
      // If user was promoted/demoted, synchronize token once silently.
      const currentToken = localStorage.getItem('arena_access_token');
      if (currentToken && profile) {
        const tokenRole = getJwtRole(currentToken);
        if (tokenRole && tokenRole !== profile.role) {
          try {
            const refreshRes = await authService.refresh();
            if (refreshRes?.token) {
              setSessionToken(refreshRes.token);
            }
          } catch {
            // Silently continue if refresh endpoint is temporarily unreachable
          }
        }
      }
    } catch (error: any) {
      if (error?.response?.status === 401) {
        setUser(null);
        setToken(null);
        localStorage.removeItem('arena_access_token');
      }
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
    <AuthContext.Provider value={{ user, token, isLoading, login, completeTwitchLogin, logout, refreshProfile, setSessionToken }}>
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
