import { FC, useState } from 'react';
import { Radio, Grid, X, Menu, Trophy, Users, BarChart2, Swords } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

export interface SidebarProps {
  isExpanded?: boolean;
  toggleSidebar?: () => void;
}

export const Sidebar: FC<SidebarProps> = ({ isExpanded = false, toggleSidebar }) => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [showLoginModal, setShowLoginModal] = useState(false);

  const containerStyle = {
    width: isExpanded ? '240px' : '64px',
    transition: 'width 0.2s ease-in-out',
    alignItems: isExpanded ? 'stretch' : 'center',
    padding: isExpanded ? '16px' : '16px 0',
  };

  const buttonClass = `rail-btn flex items-center ${isExpanded ? 'w-full justify-start px-4 gap-4' : 'justify-center'}`;

  return (
    <>
      <aside className="icon-rail" style={containerStyle as any}>
        <button className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start', marginBottom: '16px' } : { marginBottom: '16px' }} aria-label="Menu" onClick={toggleSidebar}>
          <Menu size={22} className="flex-shrink-0" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Menu</span>}
        </button>

        <Link to="/" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Home">
          <Radio size={20} className="flex-shrink-0 text-arena-crimson animate-pulse" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Live</span>}
        </Link>
        
        <Link to="/matches" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Matches">
          <Grid size={20} className="flex-shrink-0" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Matches</span>}
        </Link>
        
        <Link to="/tournaments" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Tournaments">
          <Trophy size={20} className="flex-shrink-0" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Tournaments</span>}
        </Link>

        {user?.role === 'Organizer' && (
          <>
            <Link to="/organizer/analytics" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Analytics">
              <BarChart2 size={20} className="flex-shrink-0" />
              {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Analytics</span>}
            </Link>
            <Link to="/organizer/analytics/battles" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Battle Stats">
              <Swords size={20} className="flex-shrink-0" />
              {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Battle Stats</span>}
            </Link>
            <Link to="/teams" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Teams">
              <Users size={20} className="flex-shrink-0" />
              {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Teams</span>}
            </Link>
          </>
        )}
      </aside>

      {/* Login Overlay Modal */}
      {showLoginModal && (
        <div className="fixed inset-0 z-[999] flex items-center justify-center bg-black/80 backdrop-blur-sm">
          <div className="bg-arena-surface border border-arena-border rounded-lg p-8 max-w-md w-full relative shadow-2xl">
            <button 
              className="absolute top-4 right-4 text-arena-textMuted hover:text-white"
              onClick={() => setShowLoginModal(false)}
            >
              <X size={20} />
            </button>
            <h2 className="text-2xl font-display font-bold text-white mb-2 text-center uppercase tracking-widest">Authentication Required</h2>
            <p className="text-arena-subtext text-center mb-8">
              You must be logged in to access this feature. Please sign in or create an account to continue.
            </p>
            <div className="flex flex-col gap-4">
              <button 
                onClick={() => {
                  setShowLoginModal(false);
                  navigate('/login');
                }}
                className="w-full bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold uppercase tracking-wider py-3 rounded-md transition-colors"
              >
                Log In
              </button>
              <button 
                onClick={() => {
                  setShowLoginModal(false);
                  navigate('/signup');
                }}
                className="w-full bg-transparent border border-arena-line hover:border-arena-cyan text-white font-bold uppercase tracking-wider py-3 rounded-md transition-colors"
              >
                Sign Up
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default Sidebar;
