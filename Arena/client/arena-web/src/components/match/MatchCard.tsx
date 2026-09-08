import { FC } from 'react';
import { useNavigate } from 'react-router-dom';
import { MatchResponse } from '../../types';
import Badge from '../common/Badge';
import { useAuth } from '../../context/AuthContext';
import { Video } from 'lucide-react';

interface MatchCardProps {
  match: MatchResponse;
}

export const MatchCard: FC<MatchCardProps> = ({ match }) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const date = new Date(match.scheduledTime);
  
  return (
    <div 
      onClick={() => navigate(`/matches/${match.matchId}`)}
      className="bg-arena-surface border border-arena-border rounded-sm p-5 hover:border-arena-cyan transition-all duration-200 cursor-pointer group hover:shadow-[0_0_20px_rgba(0,184,252,0.1)] relative overflow-hidden"
    >
      {/* Accent Line on hover */}
      <div className="absolute top-0 left-0 w-1 h-full bg-arena-cyan scale-y-0 group-hover:scale-y-100 transition-transform duration-300 origin-bottom"></div>
      
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
  );
};

export default MatchCard;
