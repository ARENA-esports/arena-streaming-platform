import axios from 'axios';
import { LoginResponse } from '../types';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('arena_access_token');
  if (token && config.headers) {
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
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const { data } = await axios.post<LoginResponse>(
          '/api/Auth/refresh',
          {},
          {
            baseURL: apiClient.defaults.baseURL,
            headers: {
              Authorization: `Bearer ${localStorage.getItem('arena_access_token')}`,
            },
          }
        );

        localStorage.setItem('arena_access_token', data.token);
        originalRequest.headers.Authorization = `Bearer ${data.token}`;
        
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
