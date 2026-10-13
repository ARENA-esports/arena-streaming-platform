import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { AlertCircle, Loader2 } from 'lucide-react';

export const OAuthCallbackView: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { completeTwitchLogin } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const processedRef = useRef(false);

  useEffect(() => {
    if (processedRef.current) return;
    processedRef.current = true;

    const code = searchParams.get('code');
    const err = searchParams.get('error');

    if (err) {
      setError(decodeURIComponent(err));
      return;
    }

    if (!code) {
      setError('Missing authorization code from Twitch.');
      return;
    }

    completeTwitchLogin(code)
      .then(() => {
        navigate('/', { replace: true });
      })
      .catch((e: any) => {
        setError(e?.response?.data?.message || 'Failed to complete Twitch login.');
      });
  }, [searchParams, completeTwitchLogin, navigate]);

  if (error) {
    return (
      <div className="min-h-screen bg-arena-bg flex items-center justify-center p-4">
        <div className="bg-arena-surface border border-red-500/30 rounded-2xl p-8 max-w-md w-full text-center shadow-xl">
          <div className="w-12 h-12 rounded-full bg-red-500/10 border border-red-500/20 flex items-center justify-center mx-auto mb-4 text-rose-400">
            <AlertCircle size={24} />
          </div>
          <h2 className="text-xl font-bold text-white mb-2">Twitch Login Failed</h2>
          <p className="text-sm text-arena-textMuted mb-6">{error}</p>
          <a
            href="/login"
            className="inline-block px-6 py-2.5 rounded-xl bg-arena-cyan text-zinc-900 font-bold text-sm uppercase tracking-wider hover:bg-cyan-400 transition-all"
          >
            Back to Login
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-arena-bg flex flex-col items-center justify-center p-4">
      <Loader2 size={36} className="text-[#9146FF] animate-spin mb-4" />
      <h2 className="text-lg font-bold text-white tracking-wide">Connecting to Twitch...</h2>
      <p className="text-sm text-arena-textMuted mt-1">Please wait while we log you into Arena.</p>
    </div>
  );
};

export default OAuthCallbackView;
