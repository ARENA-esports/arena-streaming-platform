import { FC } from 'react';
import { Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';

// Layouts
import DashboardLayout from '../components/layout/DashboardLayout';

// Views
import HomeView from '../views/HomeView';
import MatchesView from '../views/MatchesView';
import MatchRoomView from '../views/MatchRoomView';
import LoginView from '../views/LoginView';
import SignupView from '../views/SignupView';
import ForgotPasswordView from '../views/ForgotPasswordView';
import ResetPasswordView from '../views/ResetPasswordView';
import ScheduleMatchView from '../views/ScheduleMatchView';
import LinkStreamView from '../views/LinkStreamView';
import ForbiddenView from '../views/ForbiddenView';
import ProfileSettingsView from '../views/ProfileSettingsView';
import TournamentsView from '../views/TournamentsView';
import TeamsView from '../views/TeamsView';

export const AppRoutes: FC = () => {
  return (
    <Routes>
      {/* Public Routes with Dashboard Layout */}
      <Route element={<DashboardLayout />}>
        <Route path="/" element={<HomeView />} />
        <Route path="/matches" element={<MatchesView />} />
        <Route path="/matches/:matchId" element={<MatchRoomView />} />
        <Route path="/403" element={<ForbiddenView />} />
        
        {/* Protected Routes with Dashboard Layout */}
        <Route
          path="/teams"
          element={
            <ProtectedRoute allowedRoles={['Organizer']}>
              <TeamsView />
            </ProtectedRoute>
          }
        />
        <Route
          path="/organizer/matches/new"
          element={
            <ProtectedRoute allowedRoles={['Organizer']}>
              <ScheduleMatchView />
            </ProtectedRoute>
          }
        />
        <Route
          path="/streamer/matches/:matchId/link"
          element={
            <ProtectedRoute allowedRoles={['Streamer', 'Organizer']}>
              <LinkStreamView />
            </ProtectedRoute>
          }
        />
        <Route
          path="/settings/profile"
          element={
            <ProtectedRoute allowedRoles={['Viewer', 'Streamer', 'Organizer']}>
              <ProfileSettingsView />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tournaments"
          element={
            <ProtectedRoute allowedRoles={['Viewer', 'Streamer', 'Organizer']}>
              <TournamentsView />
            </ProtectedRoute>
          }
        />
      </Route>

      {/* Auth Routes without Dashboard Layout */}
      <Route path="/login" element={<LoginView />} />
      <Route path="/signup" element={<SignupView />} />
      <Route path="/forgot-password" element={<ForgotPasswordView />} />
      <Route path="/reset-password" element={<ResetPasswordView />} />
    </Routes>
  );
};
