import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { LogOut, User as UserIcon } from 'lucide-react';

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <nav className="bg-arena-bg border-b border-arena-border">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16">
          <div className="flex">
            <div className="flex-shrink-0 flex items-center cursor-pointer" onClick={() => navigate('/')}>
              <span className="font-display font-black text-2xl tracking-widest text-white">
                ARE<span className="text-arena-cyan">NA</span>
              </span>
            </div>
            <div className="hidden sm:ml-8 sm:flex sm:space-x-8">
              <Link
                to="/"
                className="border-transparent text-arena-textMuted hover:border-arena-cyan hover:text-white inline-flex items-center px-1 pt-1 border-b-2 text-sm font-bold uppercase tracking-wider transition-colors duration-200"
              >
                Matches
              </Link>
              {user?.role === 'Organizer' && (
                <Link
                  to="/organizer/matches/new"
                  className="border-transparent text-arena-textMuted hover:border-arena-cyan hover:text-white inline-flex items-center px-1 pt-1 border-b-2 text-sm font-bold uppercase tracking-wider transition-colors duration-200"
                >
                  Schedule Match
                </Link>
              )}
            </div>
          </div>
          <div className="hidden sm:ml-6 sm:flex sm:items-center">
            {user ? (
              <div className="flex items-center space-x-4">
                <div className="flex flex-col text-right">
                  <span className="text-sm font-bold text-white">{user.username}</span>
                  <span className="text-xs text-arena-cyan uppercase tracking-widest">{user.role}</span>
                </div>
                <div className="h-8 w-8 rounded-full bg-arena-surface border border-arena-border flex items-center justify-center">
                  <UserIcon size={16} className="text-arena-textMuted" />
                </div>
                <button
                  onClick={() => logout()}
                  className="p-1 rounded-full text-arena-textMuted hover:text-arena-crimson transition-colors duration-200 focus:outline-none"
                  title="Logout"
                >
                  <LogOut size={20} />
                </button>
              </div>
            ) : (
              <div className="flex space-x-4">
                <Link
                  to="/login"
                  className="text-white hover:text-arena-cyan px-3 py-2 text-sm font-bold uppercase tracking-wider transition-colors duration-200"
                >
                  Sign In
                </Link>
                <Link
                  to="/signup"
                  className="bg-arena-cyan text-black hover:bg-arena-cyanHover px-4 py-2 rounded-sm text-sm font-bold uppercase tracking-wider transition-colors duration-200"
                >
                  Sign Up
                </Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
