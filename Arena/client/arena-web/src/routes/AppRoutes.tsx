import React from 'react';
import { Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';

// Layouts
import Navbar from '../components/layout/Navbar';

// Views
import HomeView from '../views/HomeView';
import MatchRoomView from '../views/MatchRoomView';
import LoginView from '../views/LoginView';
import SignupView from '../views/SignupView';
import ForgotPasswordView from '../views/ForgotPasswordView';
import ResetPasswordView from '../views/ResetPasswordView';
import ScheduleMatchView from '../views/ScheduleMatchView';
import LinkStreamView from '../views/LinkStreamView';
import ForbiddenView from '../views/ForbiddenView';

export const AppRoutes: React.FC = () => {
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-grow">
        <Routes>
          {/* Public Routes */}
          <Route path="/" element={<HomeView />} />
          <Route path="/matches/:matchId" element={<MatchRoomView />} />
          <Route path="/login" element={<LoginView />} />
          <Route path="/signup" element={<SignupView />} />
          <Route path="/forgot-password" element={<ForgotPasswordView />} />
          <Route path="/reset-password" element={<ResetPasswordView />} />
          <Route path="/403" element={<ForbiddenView />} />

          {/* Protected Routes */}
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
        </Routes>
      </main>
    </div>
  );
};
