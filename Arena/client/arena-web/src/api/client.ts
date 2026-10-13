import axios from 'axios';
import { LoginResponse } from '../types';

export function normalizeBaseUrl(envUrl?: string): string {
  if (!envUrl) return '/api';
  const trimmed = envUrl.trim().replace(/\/+$/, '');
  if (!trimmed) return '/api';
  return trimmed.endsWith('/api') ? trimmed : `${trimmed}/api`;
}

export function getUserApiBase(): string {
  return normalizeBaseUrl(import.meta.env.VITE_USER_API_URL);
}

export const apiClient = axios.create({
  baseURL: '/api', // Default fallback, dynamically overridden in interceptor
  headers: {
    'Content-Type': 'application/json',
  },
  //hard comment: Automatically send and receive HttpOnly, Secure cookies with every request
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  // Dynamically set baseURL based on the requested endpoint
  const url = config.url || '';
  const lowerUrl = url.toLowerCase();
  const cleanPath = lowerUrl.startsWith('/api/') ? lowerUrl.substring(4) : lowerUrl;

  let selectedBase = '/api';

  if (cleanPath.startsWith('/auth') || cleanPath.startsWith('/users')) {
    selectedBase = normalizeBaseUrl(import.meta.env.VITE_USER_API_URL);
  } else if (cleanPath.startsWith('/tournaments') || cleanPath.startsWith('/teams')) {
    selectedBase = normalizeBaseUrl(import.meta.env.VITE_TOURNAMENT_API_URL);
  } else if (cleanPath.startsWith('/economy') || cleanPath.startsWith('/wallet')) {
    // Battle/Economy service (supports either VITE_BATTLE_API_URL or VITE_ECONOMY_API_URL)
    selectedBase = normalizeBaseUrl(import.meta.env.VITE_BATTLE_API_URL || import.meta.env.VITE_ECONOMY_API_URL);
  } else if (cleanPath.startsWith('/analytics')) {
    selectedBase = normalizeBaseUrl(import.meta.env.VITE_ANALYTICS_API_URL);
  } else if (cleanPath.startsWith('/chat')) {
    selectedBase = normalizeBaseUrl(import.meta.env.VITE_CHAT_API_URL);
  } else {
    selectedBase = normalizeBaseUrl(import.meta.env.VITE_STREAM_API_URL);
  }

  config.baseURL = selectedBase;

  // If the baseURL already includes '/api' and url also starts with '/api/', strip redundant prefix
  if (config.baseURL.endsWith('/api') && lowerUrl.startsWith('/api/')) {
    config.url = url.substring(4);
  }

  //hard: Dual-mode fallback: attach Bearer token from localStorage if present so Swagger/dev flows continue uninterrupted
  const token = localStorage.getItem('arena_access_token');
  if (token && config.headers && !config.headers.Authorization) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => {
    // If the Vite proxy fails, it might return a 504 with an HTML body instead of rejecting the promise or returning JSON.
    if (typeof response.data === 'string' && response.data.includes('<html')) {
      return Promise.reject(new Error('Received HTML instead of JSON. The backend might be down.'));
    }
    return response;
  },
  async (error) => {
    const originalRequest = error.config;
    //hard: Prevent recursive retry loops on failed authentication
    const reqUrl = originalRequest?.url?.toLowerCase() || '';
    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !reqUrl.includes('/auth/login') &&
      !reqUrl.includes('/auth/twitch')
    ) {
      originalRequest._retry = true;
      try {
        const { data } = await axios.post<LoginResponse>(
          '/Auth/refresh',
          {},
          {
            baseURL: getUserApiBase(),
            withCredentials: true, //hard: Refresh token request transmits HttpOnly session cookies
            headers: {
              Authorization: `Bearer ${localStorage.getItem('arena_access_token')}`,
            },
          }
        );

        if (data.token) {
          localStorage.setItem('arena_access_token', data.token);
          originalRequest.headers.Authorization = `Bearer ${data.token}`;
        }

        return apiClient(originalRequest);
      } catch (refreshError) {
        localStorage.removeItem('arena_access_token');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);
