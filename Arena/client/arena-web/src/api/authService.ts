import { apiClient } from './client';
import {
  SignupRequest,
  SignupResponse,
  LoginRequest,
  LoginResponse,
  UserProfile,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  ResetPasswordRequest,
  ResetPasswordResponse
} from '../types';

export const authService = {
  signup: async (data: SignupRequest) => {
    const response = await apiClient.post<SignupResponse>('/Auth/signup', data);
    return response.data;
  },
  login: async (data: LoginRequest) => {
    const response = await apiClient.post<LoginResponse>('/Auth/login', data);
    return response.data;
  },
  logout: async () => {
    const response = await apiClient.post<{ message: string }>('/Auth/logout');
    return response.data;
  },
  forgotPassword: async (data: ForgotPasswordRequest) => {
    const response = await apiClient.post<ForgotPasswordResponse>('/Auth/forgot-password', data);
    return response.data;
  },
  resetPassword: async (data: ResetPasswordRequest) => {
    const response = await apiClient.post<ResetPasswordResponse>('/Auth/reset-password', data);
    return response.data;
  },
  getMe: async () => {
    const response = await apiClient.get<UserProfile>('/Auth/me');
    return response.data;
  }
};
