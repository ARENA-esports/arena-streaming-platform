import React from 'react';
import { MatchResponse } from '../../types';
import { Trophy } from 'lucide-react';

interface EndedViewProps {
  match: MatchResponse;
}

export const EndedView: React.FC<EndedViewProps> = () => {
  return (
    <div className="flex flex-col items-center justify-center w-full aspect-video bg-arena-surface border border-arena-border rounded-sm">
      <Trophy className="w-16 h-16 text-yellow-500 mb-4 drop-shadow-[0_0_10px_rgba(234,179,8,0.4)]" />
      <h2 className="text-3xl font-bold text-white mb-2">Match Ended</h2>
      <p className="text-arena-textMuted text-sm font-mono uppercase tracking-widest">
        Thanks for watching
      </p>
    </div>
  );
};
