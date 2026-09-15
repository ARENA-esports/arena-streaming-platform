import { apiClient } from './client';
import { UserProfile } from '../types';

export interface UpdateProfileRequest {
  username?: string;
  email?: string;
  avatarUrl?: string;
  displayName?: string;
  bio?: string;
  bannerUrl?: string;
}

// //hard: Strictly type password mutation contracts to eliminate 'any' and prevent mass-assignment / unintended parameter over-posting
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// //hard: Stored XSS defense: validate URL schemes to block malicious 'javascript:' or data pseudo-protocols
const isSafeHttpUrl = (urlString?: string): boolean => {
  if (!urlString || urlString.trim() === '') return true;
  try {
    const parsed = new URL(urlString);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
};

// //hard: Client-side payload sanitization to prevent ASP.NET ModelState URL validation crashes and strip excessive whitespace
const sanitizeProfilePayload = (data: UpdateProfileRequest): UpdateProfileRequest => {
  const sanitized: UpdateProfileRequest = {};

  if (data.username !== undefined) sanitized.username = data.username.trim();
  if (data.displayName !== undefined) sanitized.displayName = data.displayName.trim();
  if (data.bio !== undefined) sanitized.bio = data.bio.trim();
  if (data.email !== undefined) sanitized.email = data.email.trim();

  // Normalize empty or whitespace-only URLs to undefined so backend validation ignores them cleanly
  if (data.avatarUrl !== undefined) {
    const trimmed = data.avatarUrl.trim();
    if (trimmed !== '') {
      if (!isSafeHttpUrl(trimmed)) {
        throw new Error('Avatar URL must use a valid http:// or https:// web address.');
      }
      sanitized.avatarUrl = trimmed;
    }
  }

  if (data.bannerUrl !== undefined) {
    const trimmed = data.bannerUrl.trim();
    if (trimmed !== '') {
      if (!isSafeHttpUrl(trimmed)) {
        throw new Error('Banner URL must use a valid http:// or https:// web address.');
      }
      sanitized.bannerUrl = trimmed;
    }
  }

  return sanitized;
};

export const userService = {
  getProfile: async () => {
    const response = await apiClient.get<UserProfile>('/Users/me');
    return response.data;
  },

  updateProfile: async (data: UpdateProfileRequest) => {
    // //hard: Pre-flight payload normalization prevents submitting malformed data or triggering backend 400 Bad Request
    const sanitizedPayload = sanitizeProfilePayload(data);
    const response = await apiClient.put<UserProfile>('/Users/me', sanitizedPayload);
    return response.data;
  },

  deleteAccount: async () => {
    const response = await apiClient.delete<{ message: string }>('/Users/me');

    // //hard: Immediate client-side credential purge ensures fallback localStorage tokens are destroyed upon account deletion
    try {
      localStorage.removeItem('arena_access_token');
    } catch {
      // Graceful fallback if localStorage is disabled or restricted
    }

    return response.data;
  },

  changePassword: async (data: ChangePasswordRequest) => {
    // //hard: Ensure non-empty password submission to prevent unnecessary backend hashing rounds on invalid inputs
    if (!data.currentPassword || !data.newPassword) {
      throw new Error('Current password and new password are required.');
    }

    const response = await apiClient.put<{ message: string }>('/Users/me/password', data);
    return response.data;
  }
};