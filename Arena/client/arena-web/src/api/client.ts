import axios from 'axios';
import { LoginResponse } from '../types';

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
  if (config.url?.startsWith('/Auth') || config.url?.startsWith('/Users')) {
    config.baseURL = import.meta.env.VITE_USER_API_URL || '/api';
  } else {
    config.baseURL = import.meta.env.VITE_STREAM_API_URL || '/api';
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
    if (error.response?.status === 401 && !originalRequest._retry && !originalRequest.url?.includes('/Auth/login')) {
      originalRequest._retry = true;
      try {
        const { data } = await axios.post<LoginResponse>(
          '/Auth/refresh',
          {},
          {
            baseURL: import.meta.env.VITE_USER_API_URL || '/api',
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
