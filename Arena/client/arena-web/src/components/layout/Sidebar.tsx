import { FC, useState } from 'react';
import { Play, Grid, Heart, X, Menu } from 'lucide-react';
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

  const handleHeartClick = () => {
    if (!user) {
      setShowLoginModal(true);
    } else {
      // Navigate to following/favorites page if it existed
      console.log('Navigate to following page');
    }
  };

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
          <Play size={20} className="flex-shrink-0" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Live</span>}
        </Link>
        
        <Link to="/matches" className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Matches">
          <Grid size={20} className="flex-shrink-0" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Matches</span>}
        </Link>
        
        <button className={buttonClass} style={isExpanded ? { width: '100%', justifyContent: 'flex-start' } : undefined} aria-label="Following" onClick={handleHeartClick}>
          <Heart size={20} className="flex-shrink-0" />
          {isExpanded && <span className="font-bold text-sm tracking-wider uppercase">Following</span>}
        </button>
        
        <div className={`rail-divider ${isExpanded ? 'w-full' : ''}`}></div>
        
        <div className={`rail-avatars ${isExpanded ? 'w-full px-2' : ''}`}>
          {['HV', 'AR', 'CV', 'FX', 'WC', 'SI', 'EA'].map((initials, idx) => (
            <div 
              key={idx} 
              className={`rail-avatar ${['HV', 'AR', 'FX'].includes(initials) ? 'live' : ''} ${isExpanded ? 'w-full justify-start px-3 gap-3 rounded-md' : ''}`}
              style={isExpanded ? { width: '100%', borderRadius: '8px' } : undefined}
            >
              <span className={isExpanded ? '' : 'hidden'}>{['HV', 'AR', 'FX'].includes(initials) ? '🔴' : ''}</span>
              {initials}
              {isExpanded && <span className="font-sans text-xs capitalize text-arena-subtext ml-2">Channel</span>}
            </div>
          ))}
        </div>
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
