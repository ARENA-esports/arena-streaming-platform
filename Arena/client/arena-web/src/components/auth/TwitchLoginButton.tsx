import React from 'react';

export const TwitchLoginButton: React.FC = () => {
  const handleTwitchClick = () => {
    // Initiate full-page browser challenge
    window.location.href = '/api/auth/twitch';
  };

  return (
    <div className="w-full my-4">
      <button
        id="btn-login-twitch"
        type="button"
        onClick={handleTwitchClick}
        className="w-full flex items-center justify-center gap-3 py-2.5 px-4 rounded-xl bg-[#9146FF] hover:bg-[#7d2bef] text-white font-medium text-sm transition-all shadow-md cursor-pointer"
      >
        <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
          <path d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z" />
        </svg>
        <span>Continue with Twitch</span>
      </button>

      <div className="flex items-center my-4">
        <div className="flex-grow border-t border-zinc-800"></div>
        <span className="px-3 text-xs text-zinc-500 uppercase tracking-wider font-mono">or</span>
        <div className="flex-grow border-t border-zinc-800"></div>
      </div>
    </div>
  );
};
