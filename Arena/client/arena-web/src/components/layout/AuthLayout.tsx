import { Link } from 'react-router-dom';
import { useTheme } from '../../context/ThemeContext';
import { Moon, Sun, Monitor } from 'lucide-react';

interface AuthLayoutProps {
  children: React.ReactNode;
  title: string;
  subtitle?: string;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children, title, subtitle }) => {
  const { theme, toggleTheme } = useTheme();

  return (
    <div className="min-h-screen flex flex-col bg-[var(--bg)] transition-colors">
      {/* Minimal Topbar */}
      <header className="flex items-center justify-between h-16 px-6 border-b border-[var(--line)] bg-[var(--topbar-bg)] transition-colors">
        <Link to="/" className="font-extrabold text-xl tracking-tighter text-[var(--text)] text-decoration-none">
          aren<span className="text-[var(--prime)]">a</span>
        </Link>
        
        {/* Animated Theme Switcher */}
        <button 
          className="flex items-center gap-3 px-3 py-1.5 rounded-full hover:bg-[var(--panel-2)] text-sm text-[var(--text)] outline-none focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--prime)] transition-colors border border-transparent hover:border-[var(--line)]"
          onClick={toggleTheme}
        >
          <div className="flex items-center gap-2">
            {theme === 'system' ? <Monitor size={16} /> : theme === 'dark' ? <Moon size={16} /> : <Sun size={16} />}
            <span className="hidden sm:inline text-xs font-semibold uppercase tracking-widest">{theme}</span>
          </div>
          <div className={`w-8 h-4 rounded-full p-0.5 transition-colors ${theme === 'dark' ? 'bg-[#9146FF]' : theme === 'light' ? 'bg-[var(--prime)]' : 'bg-zinc-500'}`}>
            <div className={`w-3 h-3 rounded-full bg-white transition-transform ${theme === 'dark' ? 'translate-x-4' : theme === 'light' ? 'translate-x-0' : 'translate-x-2'}`} />
          </div>
        </button>
      </header>

      {/* Main Content */}
      <main className="flex-1 flex flex-col items-center justify-center p-4">
        <div className="w-full max-w-md bg-[var(--panel)] border border-[var(--line)] rounded-xl p-8 shadow-2xl transition-colors">
          <div className="mb-8">
            <h2 className="text-center text-[var(--text)] font-extrabold tracking-widest text-2xl uppercase">
              {title}
            </h2>
            {subtitle && (
              <p className="mt-2 text-center text-[13px] text-[var(--subtext)]">
                {subtitle}
              </p>
            )}
          </div>
          {children}
        </div>
      </main>
    </div>
  );
};

export default AuthLayout;
