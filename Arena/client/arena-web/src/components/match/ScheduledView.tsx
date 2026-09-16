import React, { useState, useEffect } from 'react';
import { MatchResponse } from '../../types';

interface ScheduledViewProps {
  match: MatchResponse;
}

export const ScheduledView: React.FC<ScheduledViewProps> = ({ match }) => {
  const [timeLeft, setTimeLeft] = useState<string>('');

  useEffect(() => {
    const calculateTimeLeft = () => {
      const difference = new Date(match.scheduledTime).getTime() - new Date().getTime();

      if (difference <= 0) {
        return '00:00:00';
      }

      const days = Math.floor(difference / (1000 * 60 * 60 * 24));
      const hours = Math.floor((difference / (1000 * 60 * 60)) % 24);
      const minutes = Math.floor((difference / 1000 / 60) % 60);
      const seconds = Math.floor((difference / 1000) % 60);

      if (days > 0) {
        return `${days.toString().padStart(2, '0')}:${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
      }

      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    };

    setTimeLeft(calculateTimeLeft());
    const id = setInterval(() => setTimeLeft(calculateTimeLeft()), 1000);
    return () => clearInterval(id);
  }, [match.scheduledTime]);

  return (
    <div className="flex flex-col items-center justify-center w-full aspect-video bg-arena-surface border border-arena-border rounded-sm">
      <div className="flex flex-col items-center space-y-4">
        <div className="text-lg font-mono text-arena-textMuted uppercase tracking-widest">
          Stream starts in
        </div>
        <div className="text-5xl md:text-7xl font-mono font-bold text-arena-cyan drop-shadow-[0_0_15px_rgba(0,184,252,0.4)]">
          {timeLeft}
        </div>

        <div className="flex items-center space-x-6 mt-8">
          <div className="text-xl font-bold text-white">Team {match.teamAId}</div>
          <div className="text-arena-textMuted italic">VS</div>
          <div className="text-xl font-bold text-white">Team {match.teamBId}</div>
        </div>
      </div>
    </div>
  );
};
