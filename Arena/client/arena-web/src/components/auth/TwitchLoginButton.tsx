import React, { useState } from 'react';
import { getUserApiBase } from '../../api/client';

interface TwitchLoginButtonProps {
  label?: string;
}

export const TwitchLoginButton: React.FC<TwitchLoginButtonProps> = ({
  label = 'Continue with Twitch',
}) => {
  const [loading, setLoading] = useState(false);

  const handleTwitchClick = () => {
    setLoading(true);
    // Initiate full-page browser challenge against User Service
    const base = getUserApiBase();
    window.location.href = `${base}/auth/twitch`;
  };

  return (
    <div className="w-full my-4">
      <button
        id="btn-login-twitch"
        type="button"
        onClick={handleTwitchClick}
        disabled={loading}
        className={`w-full flex items-center justify-center gap-3 py-2.5 px-4 rounded-xl bg-[#9146FF] hover:bg-[#7d2bef] active:bg-[#6c1fd6] text-white font-medium text-sm transition-all shadow-md cursor-pointer ${
          loading ? 'opacity-70 cursor-not-allowed' : ''
        }`}
      >
        {loading ? (
          <svg className="animate-spin h-4 w-4 text-white" viewBox="0 0 24 24" fill="none">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
          </svg>
        ) : (
          <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
            <path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z" />
          </svg>
        )}
        <span>{loading ? 'Connecting to Twitch...' : label}</span>
      </button>

      <div className="flex items-center my-4">
        <div className="flex-grow border-t border-zinc-800"></div>
        <span className="px-3 text-xs text-zinc-500 uppercase tracking-wider font-mono">or</span>
        <div className="flex-grow border-t border-zinc-800"></div>
      </div>
    </div>
  );
};

