import React from 'react';
import { MatchResponse } from '../../types';
import { XCircle } from 'lucide-react';

interface CancelledViewProps {
  match: MatchResponse;
}

export const CancelledView: React.FC<CancelledViewProps> = () => {
  return (
    <div className="flex flex-col items-center justify-center w-full aspect-video bg-arena-surface border border-arena-crimson/50 rounded-sm">
      <XCircle className="w-16 h-16 text-arena-crimson mb-4 drop-shadow-[0_0_10px_rgba(255,51,102,0.4)]" />
      <h2 className="text-3xl font-bold text-white mb-2">Match Cancelled</h2>
      <p className="text-arena-crimson text-sm font-mono uppercase tracking-widest">
        This event will not take place
      </p>
    </div>
  );
};
