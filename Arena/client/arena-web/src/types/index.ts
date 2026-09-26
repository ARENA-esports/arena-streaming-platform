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
  emailVerified?: boolean;
  avatarUrl?: string;
  displayName?: string;
  bio?: string;
  bannerUrl?: string;
  createdAt?: string;
  updatedAt?: string;
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
  status: 'Scheduled' | 'Live' | 'Ended' | 'Cancelled';
}

export interface TeamSummary {
  teamId: number;
  name: string;
  colorHex: string;
  logoUrl: string | null;
}

export interface MatchScheduleResponse {
  matchId: number;
  tournamentId: number;
  scheduledTime: string;
  status: 'Scheduled' | 'Live' | 'Ended' | 'Cancelled';
  teamA: TeamSummary;
  teamB: TeamSummary;
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

export interface UpdateMatchRequest {
  teamAId: number;
  teamBId: number;
  scheduledTime: string;
}

export interface UpdateMatchStatusRequest {
  status: 'Scheduled' | 'Live' | 'Ended' | 'Cancelled';
  forceOverride: boolean;
}

export interface TournamentResponse {
  id: number;
  name: string;
  season_identifier: string;
  start_date: string;
  end_date: string;
  status: 'Scheduled' | 'Active' | 'Completed' | 'Cancelled';
}

export interface CreateTournamentRequest {
  name: string;
  season_identifier: string;
  start_date: string;
  end_date: string;
}

export interface UpdateTournamentRequest {
  name: string;
  season_identifier: string;
  start_date: string;
  end_date: string;
}

// ── Chat Types ──

export interface ChatMessage {
  messageId: number;
  teamId: number;
  teamName: string;
  teamColor: string;
  userId: number;
  username: string;
  content: string;
  createdAt: string;
}

export interface ChatHistoryFrame {
  type: 'history';
  messages: ChatMessage[];
}

export interface ChatMessageFrame extends ChatMessage {
  type: 'message';
}

export interface ChatErrorFrame {
  type: 'error';
  message: string;
}

export type ChatFrame = ChatHistoryFrame | ChatMessageFrame | ChatErrorFrame;
