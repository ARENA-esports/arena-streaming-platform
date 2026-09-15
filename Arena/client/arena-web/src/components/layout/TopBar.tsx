import { FC, useState, useRef, useEffect } from 'react';
import { Search, User, Settings, LogOut, Video, BarChart2, Moon, Sun, Monitor } from 'lucide-react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';

export const TopBar: FC = () => {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    setDropdownOpen(false);
    logout();
  };

  const [searchQuery, setSearchQuery] = useState('');

  const handleSearch = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && searchQuery.trim()) {
      navigate(`/matches?teamId=${encodeURIComponent(searchQuery.trim())}`);
    }
  };

  return (
    <header className="topbar">
      <Link to="/" className="brand" style={{ marginLeft: '24px' }}>aren<span>a</span></Link>
      <div className="search-wrap hidden sm:block">
        <Search size={16} />
        <input 
          type="text" 
          placeholder="Search by Team ID..." 
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          onKeyDown={handleSearch}
        />
      </div>
      <div className="topbar-right relative">
        {user ? (
          <div className="flex items-center gap-4 relative" ref={dropdownRef}>
            {user.role === 'Organizer' && (
              <Link to="/organizer/matches/new" className="btn-ghost" style={{ textDecoration: 'none' }}>
                Schedule
              </Link>
            )}
            
            <button 
              className="w-[34px] h-[34px] rounded-full bg-[var(--panel)] border border-[var(--line)] text-[var(--text)] flex items-center justify-center hover:border-[var(--prime)] transition-colors outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]"
              onClick={() => setDropdownOpen(!dropdownOpen)}
            >
              {(user.avatarUrl && (user.avatarUrl.startsWith('http://') || user.avatarUrl.startsWith('https://'))) ? (
                <img src={user.avatarUrl} alt={user.username} className="w-full h-full rounded-full object-cover" />
              ) : (
                <User size={18} />
              )}
            </button>

            {dropdownOpen && (
              <div className="absolute top-12 right-0 w-64 bg-arena-surface border border-arena-border rounded-md shadow-lg py-2 z-50 animate-in fade-in slide-in-from-top-2 duration-200">
                
                {/* Profile Header */}
                <div 
                  className="flex items-center gap-3 px-4 py-3 hover:bg-[var(--panel-2)] cursor-pointer border-b border-[var(--line)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]"
                  tabIndex={0}
                  onClick={() => {
                    setDropdownOpen(false);
                    navigate('/settings/profile');
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      setDropdownOpen(false);
                      navigate('/settings/profile');
                    }
                  }}
                >
                  <div className="w-10 h-10 rounded-full bg-arena-cyan text-black flex items-center justify-center flex-shrink-0">
                    {(user.avatarUrl && (user.avatarUrl.startsWith('http://') || user.avatarUrl.startsWith('https://'))) ? (
                      <img src={user.avatarUrl} alt={user.username} className="w-full h-full rounded-full object-cover" />
                    ) : (
                      <User size={24} />
                    )}
                  </div>
                  <div className="flex flex-col truncate">
                    <span className="font-bold text-sm text-arena-text truncate">{user.displayName || user.username}</span>
                    <span className="text-xs text-arena-textMuted capitalize">{user.role}</span>
                  </div>
                </div>

                {/* Main Links */}
                <div className="py-1 border-b border-[var(--line)]">
                  <Link to="#" className="flex items-center gap-3 px-4 py-2 hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]" onClick={() => setDropdownOpen(false)}>
                    <Video size={18} />
                    Channel
                  </Link>
                  <Link to="#" className="flex items-center gap-3 px-4 py-2 hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]" onClick={() => setDropdownOpen(false)}>
                    <BarChart2 size={18} />
                    Creator Dashboard
                  </Link>
                </div>

                {/* Settings & Theme */}
                <div className="py-1 border-b border-[var(--line)]">
                  <Link to="/settings/profile" className="flex items-center gap-3 px-4 py-2 hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]" onClick={() => setDropdownOpen(false)}>
                    <Settings size={18} />
                    Settings
                  </Link>
                  
                  <button 
                    className="w-full flex items-center justify-between px-4 py-2 hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)] mt-1 border-t border-[var(--line)]"
                    onClick={toggleTheme}
                  >
                    <div className="flex items-center gap-3">
                      {theme === 'system' ? <Monitor size={18} /> : theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
                      {theme === 'system' ? 'System Theme' : theme === 'dark' ? 'Dark Theme' : 'Light Theme'}
                    </div>
                    <div className={`w-8 h-4 rounded-full p-0.5 transition-colors ${theme === 'dark' ? 'bg-[#9146FF]' : theme === 'light' ? 'bg-[var(--prime)]' : 'bg-zinc-500'}`}>
                      <div className={`w-3 h-3 rounded-full bg-white transition-transform ${theme === 'dark' ? 'translate-x-4' : theme === 'light' ? 'translate-x-0' : 'translate-x-2'}`} />
                    </div>
                  </button>
                </div>

                {/* Logout */}
                <div className="py-1">
                  <button 
                    className="w-full flex items-center gap-3 px-4 py-2 hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]"
                    onClick={handleLogout}
                  >
                    <LogOut size={18} />
                    Log Out
                  </button>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div className="flex items-center gap-4 relative" ref={dropdownRef}>
            {location.pathname !== '/login' && (
              <Link to="/login" className="btn-ghost" style={{ textDecoration: 'none' }}>Log In</Link>
            )}
            {location.pathname !== '/signup' && (
              <Link to="/signup" className="btn-solid" style={{ textDecoration: 'none' }}>Sign Up</Link>
            )}
            
            <button 
              className="w-[34px] h-[34px] rounded-full bg-[var(--panel)] border border-[var(--line)] text-[var(--text)] flex items-center justify-center hover:border-[var(--prime)] transition-colors outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]"
              onClick={() => setDropdownOpen(!dropdownOpen)}
            >
              <User size={18} />
            </button>

            {dropdownOpen && (
              <div className="absolute top-12 right-0 w-64 bg-arena-surface border border-arena-border rounded-md shadow-lg py-2 z-50 animate-in fade-in slide-in-from-top-2 duration-200">
                <div className="py-1 border-b border-arena-border">
                  <button 
                    className="w-full flex items-center justify-between px-4 py-2 hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)]"
                    onClick={toggleTheme}
                  >
                    <div className="flex items-center gap-3">
                      {theme === 'system' ? <Monitor size={18} /> : theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
                      {theme === 'system' ? 'System Theme' : theme === 'dark' ? 'Dark Theme' : 'Light Theme'}
                    </div>
                    <div className={`w-8 h-4 rounded-full p-0.5 transition-colors ${theme === 'dark' ? 'bg-[#9146FF]' : theme === 'light' ? 'bg-[var(--prime)]' : 'bg-zinc-500'}`}>
                      <div className={`w-3 h-3 rounded-full bg-white transition-transform ${theme === 'dark' ? 'translate-x-4' : theme === 'light' ? 'translate-x-0' : 'translate-x-2'}`} />
                    </div>
                  </button>
                </div>
                <div className="py-1">
                  <Link 
                    to="/login"
                    className="flex items-center gap-3 px-4 py-2 hover:bg-arena-surfaceHover text-sm text-arena-text"
                    onClick={() => setDropdownOpen(false)}
                  >
                    <LogOut size={18} className="rotate-180" />
                    Log In
                  </Link>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
};

export default TopBar;
