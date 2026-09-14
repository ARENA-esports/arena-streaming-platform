import { FC, useState, useRef, useEffect } from 'react';
import { Search, User, Settings, LogOut, Moon, Sun, Video, BarChart2 } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';

export const TopBar: FC = () => {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
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

  return (
    <header className="topbar">
      <Link to="/" className="brand" style={{ marginLeft: '24px' }}>aren<span>a</span></Link>
      <div className="search-wrap">
        <Search size={16} />
        <input type="text" placeholder="Search matches, teams, streamers" />
      </div>
      <div className="topbar-right relative">
        {user ? (
          <div className="flex items-center gap-4 relative" ref={dropdownRef}>
            {user.role === 'Organizer' && (
              <Link to="/organizer/matches/new" className="btn-ghost" style={{ textDecoration: 'none' }}>
                Schedule Match
              </Link>
            )}
            
            <button 
              className="w-8 h-8 rounded-full bg-arena-cyan text-black flex items-center justify-center hover:bg-arena-cyanHover transition-colors focus:outline-none focus:ring-2 focus:ring-arena-cyanFocus"
              onClick={() => setDropdownOpen(!dropdownOpen)}
            >
              {user.avatarUrl ? (
                <img src={user.avatarUrl} alt={user.username} className="w-full h-full rounded-full object-cover" />
              ) : (
                <User size={20} />
              )}
            </button>

            {dropdownOpen && (
              <div className="absolute top-12 right-0 w-64 bg-arena-surface border border-arena-border rounded-md shadow-lg py-2 z-50 animate-in fade-in slide-in-from-top-2 duration-200">
                
                {/* Profile Header */}
                <div 
                  className="flex items-center gap-3 px-4 py-3 hover:bg-arena-surfaceHover cursor-pointer border-b border-arena-border"
                  onClick={() => {
                    setDropdownOpen(false);
                    navigate('/settings/profile');
                  }}
                >
                  <div className="w-10 h-10 rounded-full bg-arena-cyan text-black flex items-center justify-center flex-shrink-0">
                    {user.avatarUrl ? (
                      <img src={user.avatarUrl} alt={user.username} className="w-full h-full rounded-full object-cover" />
                    ) : (
                      <User size={24} />
                    )}
                  </div>
                  <div className="flex flex-col truncate">
                    <span className="font-bold text-sm text-white truncate">{user.displayName || user.username}</span>
                    <span className="text-xs text-arena-textMuted capitalize">{user.role}</span>
                  </div>
                </div>

                {/* Main Links */}
                <div className="py-1 border-b border-arena-border">
                  <Link to="#" className="flex items-center gap-3 px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300" onClick={() => setDropdownOpen(false)}>
                    <Video size={18} />
                    Channel
                  </Link>
                  <Link to="#" className="flex items-center gap-3 px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300" onClick={() => setDropdownOpen(false)}>
                    <BarChart2 size={18} />
                    Creator Dashboard
                  </Link>
                </div>

                {/* Settings & Theme */}
                <div className="py-1 border-b border-arena-border">
                  <Link to="/settings/profile" className="flex items-center gap-3 px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300" onClick={() => setDropdownOpen(false)}>
                    <Settings size={18} />
                    Settings
                  </Link>
                  <button 
                    className="w-full flex items-center justify-between px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300"
                    onClick={toggleTheme}
                  >
                    <div className="flex items-center gap-3">
                      {theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
                      Dark Theme
                    </div>
                    <div className={`w-8 h-4 rounded-full p-0.5 transition-colors ${theme === 'dark' ? 'bg-purple-600' : 'bg-gray-600'}`}>
                      <div className={`w-3 h-3 rounded-full bg-white transition-transform ${theme === 'dark' ? 'translate-x-4' : 'translate-x-0'}`} />
                    </div>
                  </button>
                </div>

                {/* Logout */}
                <div className="py-1">
                  <button 
                    className="w-full flex items-center gap-3 px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300"
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
            <Link to="/login" className="btn-ghost" style={{ textDecoration: 'none' }}>Log In</Link>
            <Link to="/signup" className="btn-solid" style={{ textDecoration: 'none' }}>Sign Up</Link>
            
            <button 
              className="w-8 h-8 rounded-full bg-arena-surface border border-arena-border text-white flex items-center justify-center hover:bg-arena-surfaceHover transition-colors focus:outline-none"
              onClick={() => setDropdownOpen(!dropdownOpen)}
            >
              <User size={20} />
            </button>

            {dropdownOpen && (
              <div className="absolute top-12 right-0 w-56 bg-arena-surface border border-arena-border rounded-md shadow-lg py-2 z-50 animate-in fade-in slide-in-from-top-2 duration-200">
                <div className="py-1 border-b border-arena-border">
                  <button 
                    className="w-full flex items-center justify-between px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300"
                    onClick={toggleTheme}
                  >
                    <div className="flex items-center gap-3">
                      {theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
                      Dark Theme
                    </div>
                    <div className={`w-8 h-4 rounded-full p-0.5 transition-colors ${theme === 'dark' ? 'bg-purple-600' : 'bg-gray-600'}`}>
                      <div className={`w-3 h-3 rounded-full bg-white transition-transform ${theme === 'dark' ? 'translate-x-4' : 'translate-x-0'}`} />
                    </div>
                  </button>
                </div>
                <div className="py-1">
                  <Link 
                    to="/login"
                    className="flex items-center gap-3 px-4 py-2 hover:bg-arena-surfaceHover text-sm text-gray-300"
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
