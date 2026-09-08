import { FC } from 'react';
import { Search, Globe } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

export const TopBar: FC = () => {
  const { user, logout } = useAuth();

  return (
    <header className="topbar">
      <Link to="/" className="brand" style={{ marginLeft: '24px' }}>aren<span>a</span></Link>
      <div className="search-wrap">
        <Search size={16} />
        <input type="text" placeholder="Search matches, teams, streamers" />
      </div>
      <div className="topbar-right">
        <button className="icon-btn" aria-label="Language">
          <Globe size={19} />
        </button>
        {user ? (
          <div className="flex items-center gap-4">
            {user.role === 'Organizer' && (
              <Link to="/organizer/matches/new" className="btn-ghost" style={{ textDecoration: 'none' }}>
                Schedule Match
              </Link>
            )}
            <div className="flex flex-col items-end leading-tight">
              <span className="font-mono text-sm font-bold uppercase tracking-wider text-arena-cyan">{user.username}</span>
              <span className="text-[10px] text-arena-textMuted uppercase font-bold tracking-widest">{user.role}</span>
            </div>
            <button onClick={logout} className="btn-ghost">Log Out</button>
          </div>
        ) : (
          <>
            <Link to="/login" className="btn-ghost" style={{ textDecoration: 'none' }}>Log In</Link>
            <Link to="/signup" className="btn-solid" style={{ textDecoration: 'none' }}>Sign Up</Link>
          </>
        )}
      </div>
    </header>
  );
};

export default TopBar;
