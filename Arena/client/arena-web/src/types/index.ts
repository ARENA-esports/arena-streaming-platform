export interface SignupRequest {
  username: string;
  email: string;
  password: string;
}

export interface SignupResponse {
  userId: number;
  username: string;
  email: string;
  message: string;
}

export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  tokenType: string;
  expiresIn: number;
  userId: number;
  username: string;
  email: string;
  role: 'Viewer' | 'Streamer' | 'Organizer';
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
  resetToken?: string;
  expiresAt?: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface ResetPasswordResponse {
  message: string;
}

export interface UserProfile {
  userId: number;
  username: string;
  email: string;
  role: 'Viewer' | 'Streamer' | 'Organizer';
}

export interface CreateMatchRequest {
  teamAId: number;
  teamBId: number;
  scheduledTime: string;
}

export interface MatchResponse {
  matchId: number;
  teamAId: number;
  teamBId: number;
  scheduledTime: string;
  status: 'Scheduled' | 'Live' | 'Ended';
}

export interface LinkStreamRequest {
  channelName: string;
  embedParentDomain: string;
}

export interface StreamResponse {
  id: number;
  matchId: number;
  channelName: string;
  status: 'Scheduled' | 'Live' | 'Ended';
  twitchUrl: string;
  startedAt?: string | null;
  endedAt?: string | null;
}
