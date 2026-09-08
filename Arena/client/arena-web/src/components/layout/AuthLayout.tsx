import React from 'react';

interface AuthLayoutProps {
  children: React.ReactNode;
  title: string;
  subtitle?: string;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children, title, subtitle }) => {
  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center bg-arena-bg py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8 bg-arena-surface p-10 rounded-sm border border-arena-border shadow-[0_0_50px_rgba(0,184,252,0.05)]">
        <div>
          <h2 className="text-center text-3xl font-display font-black text-white tracking-widest uppercase">
            {title}
          </h2>
          {subtitle && (
            <p className="mt-2 text-center text-sm text-arena-textMuted font-sans">
              {subtitle}
            </p>
          )}
        </div>
        {children}
      </div>
    </div>
  );
};

export default AuthLayout;
