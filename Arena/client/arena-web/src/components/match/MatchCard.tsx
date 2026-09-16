import { FC, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MatchScheduleResponse, TeamSummary } from '../../types';
import Badge from '../common/Badge';
import { useAuth } from '../../context/AuthContext';
import { formatLogoUrl } from '../../api/teamService';
import { Video } from 'lucide-react';

const ACCENT_PALETTE = [
  '#00F5FF', // Electric Cyan
  '#FF0000', // Vivid Crimson
  '#9146FF', // Twitch Purple
  '#1F69FF', // Royal Blue
  '#00FF00', // Neon Lime
] as const;

interface MatchCardProps {
  match: MatchScheduleResponse;
}

const MatchTeamLogo: FC<{ team: TeamSummary }> = ({ team }) => {
  const [imgError, setImgError] = useState(false);
  const formattedUrl = formatLogoUrl(team.logoUrl);
  const isInvalidUrl = !formattedUrl || formattedUrl.includes('assets.arena.gg');
  const showImg = !isInvalidUrl && !imgError;

  return (
    <div 
      className="w-16 h-16 rounded-full border-2 flex items-center justify-center shadow-inner overflow-hidden bg-[var(--panel-2)] shrink-0"
      style={{ borderColor: team.colorHex || '#00B8FC' }}
    >
      {showImg ? (
        <img 
          src={formattedUrl} 
          alt={team.name} 
          className="w-full h-full object-cover" 
          onError={() => setImgError(true)}
        />
      ) : (
        <span className="text-lg font-bold text-white" style={{ color: team.colorHex || '#FFF' }}>
          {(team.name || `T${team.teamId}`).substring(0, 2).toUpperCase()}
        </span>
      )}
    </div>
  );
};

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
      {/* Underlay container */}
      <div className="relative w-full rounded-sm" style={{ backgroundColor: accentColor }}>
        {/* Shifted surface layer */}
        <div className="relative z-10 w-full h-full bg-[var(--panel)] border border-[var(--line)] transform transition-transform duration-150 ease-out group-hover:-translate-y-1.5 group-hover:translate-x-1.5 flex flex-col justify-between overflow-hidden p-5">
          <div className="flex justify-between items-start mb-4">
            <Badge status={match.status} />
            <span className="text-xs text-arena-textMuted font-mono">
              {date.toLocaleDateString()} • {date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
            </span>
          </div>

          <div className="flex justify-center items-center space-x-4 my-6">
            <div className="flex flex-col items-center gap-2">
              <MatchTeamLogo team={match.teamA} />
              <span className="text-xs font-mono font-bold text-[var(--text)]">{match.teamA.name}</span>
            </div>
            <div className="text-[var(--subtext)] font-bold text-xl italic px-2">VS</div>
            <div className="flex flex-col items-center gap-2">
              <MatchTeamLogo team={match.teamB} />
              <span className="text-xs font-mono font-bold text-[var(--text)]">{match.teamB.name}</span>
            </div>
          </div>

          <div className="mt-4 pt-4 border-t border-arena-border flex justify-between items-center">
            <span className="text-sm font-semibold text-arena-cyan group-hover:text-[var(--text)] transition-colors">
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
    </div>
  );
};

export default MatchCard;
// single match card