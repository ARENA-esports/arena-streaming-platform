import { FC, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MatchResponse } from '../../types';
import Badge from '../common/Badge';
import { useAuth } from '../../context/AuthContext';
import { Video } from 'lucide-react';

const ACCENT_PALETTE = [
  '#00F5FF', // Electric Cyan
  '#FF0000', // Vivid Crimson
  '#9146FF', // Twitch Purple
  '#1F69FF', // Royal Blue
  '#00FF00', // Neon Lime
] as const;

interface MatchCardProps {
  match: MatchResponse;
}

export const MatchCard: FC<MatchCardProps> = ({ match }) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const date = new Date(match.scheduledTime);
  const [accentColor] = useState(() => ACCENT_PALETTE[Math.floor(Math.random() * ACCENT_PALETTE.length)]);

  return (
    <div
      onClick={() => navigate(`/matches/${match.matchId}`)}
      className="group relative w-full cursor-pointer rounded-sm mb-4"
    >
      {/* Underlay Container */}
      <div 
        className="absolute inset-0 rounded-sm"
        style={{ backgroundColor: accentColor }}
      />
      
      {/* Thumbnail Surface */}
      <div className="relative z-10 w-full h-full bg-[#0D1117] border border-arena-border rounded-sm p-5 transition-transform duration-150 ease-out group-hover:-translate-y-1.5 group-hover:translate-x-1.5 flex flex-col justify-between overflow-hidden">

      <div className="flex justify-between items-start mb-4">
        <Badge status={match.status} />
        <span className="text-xs text-arena-textMuted font-mono">
          {date.toLocaleDateString()} • {date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </span>
      </div>

      <div className="flex justify-center items-center space-x-4 my-6">
        <div className="flex flex-col items-center">
          <div className="w-16 h-16 rounded-full bg-arena-bg border border-arena-border flex items-center justify-center shadow-inner">
            <span className="text-xl font-bold text-white">T{match.teamAId}</span>
          </div>
        </div>
        <div className="text-arena-textMuted font-bold text-xl italic">VS</div>
        <div className="flex flex-col items-center">
          <div className="w-16 h-16 rounded-full bg-arena-bg border border-arena-border flex items-center justify-center shadow-inner">
            <span className="text-xl font-bold text-white">T{match.teamBId}</span>
          </div>
        </div>
      </div>

      <div className="mt-4 pt-4 border-t border-arena-border flex justify-between items-center">
        <span className="text-sm font-semibold text-arena-cyan group-hover:text-white transition-colors">
          View Details
        </span>
        {(user?.role === 'Streamer' || user?.role === 'Organizer') && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/streamer/matches/${match.matchId}/link`);
            }}
            className="text-sm font-semibold flex items-center text-arena-textMuted hover:text-arena-cyan transition-colors"
          >
            <Video size={14} className="mr-1" /> Link Stream
          </button>
        )}
      </div>
      </div>
    </div>
  );
};

export default MatchCard;
// single match card