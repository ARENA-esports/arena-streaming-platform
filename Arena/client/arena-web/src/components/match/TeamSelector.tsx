import React, { useState, useEffect } from 'react';
import { Users, Shield } from 'lucide-react';
import { teamService, Team, formatLogoUrl } from '../../api/teamService';
import { matchService } from '../../api/matchService';

interface TeamSelectorProps {
  matchId: number | string;
  onTeamSelect?: (teamId: number | null) => void;
}

export const TeamSelector: React.FC<TeamSelectorProps> = ({ matchId, onTeamSelect }) => {
  const [teamA, setTeamA] = useState<Team | null>(null);
  const [teamB, setTeamB] = useState<Team | null>(null);
  const [selectedTeamId, setSelectedTeamId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;
    async function fetchTeams() {
      try {
        setLoading(true);
        const match = await matchService.getMatch(Number(matchId));
        const [a, b] = await Promise.all([
          teamService.getTeamById(match.teamAId),
          teamService.getTeamById(match.teamBId),
        ]);
        if (isMounted) {
          setTeamA(a);
          setTeamB(b);
        }
      } catch (err) {
        console.error('[TeamSelector] Failed to load teams', err);
      } finally {
        if (isMounted) setLoading(false);
      }
    }
    fetchTeams();
    return () => { isMounted = false; };
  }, [matchId]);

  const handleSelect = (teamId: number) => {
    const newId = selectedTeamId === teamId ? null : teamId;
    setSelectedTeamId(newId);
    onTeamSelect?.(newId);
  };

  if (loading) {
    return (
      <div className="w-full bg-arena-surface border border-arena-border rounded-[14px] p-4 flex items-center justify-center">
        <div className="w-5 h-5 border-2 border-arena-cyan border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!teamA || !teamB) {
    return (
      <div className="w-full bg-arena-surface border border-arena-border rounded-[14px] p-4 text-sm text-arena-textMuted text-center">
        Team data unavailable
      </div>
    );
  }

  return (
    <div className="w-full bg-arena-surface border border-arena-border rounded-[14px] overflow-hidden">
      {/* Header */}
      <div className="flex items-center gap-2 px-4 py-2.5 border-b border-arena-border">
        <Shield size={14} className="text-arena-cyan" />
        <span className="text-xs font-bold uppercase tracking-wider text-arena-text">
          Choose Your Faction
        </span>
      </div>

      {/* Team cards */}
      <div className="flex gap-2 p-3">
        {[teamA, teamB].map((team) => {
          const isSelected = selectedTeamId === team.teamId;
          const logoSrc = team.logoUrl ? formatLogoUrl(team.logoUrl) : null;

          return (
            <button
              key={team.teamId}
              onClick={() => handleSelect(team.teamId)}
              className={`
                flex-1 flex items-center gap-3 p-3 rounded-xl border-2 transition-all duration-200 cursor-pointer
                ${isSelected
                  ? 'border-arena-cyan bg-arena-cyan/5 shadow-[0_0_12px_-3px_rgba(0,184,252,0.3)]'
                  : 'border-arena-border hover:border-arena-textMuted bg-arena-bg/50 hover:bg-arena-surfaceHover/40'
                }
              `}
              id={`team-select-${team.teamId}`}
            >
              {/* Team logo / color swatch */}
              <div
                className="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0 overflow-hidden"
                style={{ backgroundColor: team.colorHex || '#2A2F38' }}
              >
                {logoSrc ? (
                  <img src={logoSrc} alt={team.name} className="w-full h-full object-cover" />
                ) : (
                  <Users size={16} className="text-white/80" />
                )}
              </div>

              <div className="text-left min-w-0">
                <p className={`text-sm font-semibold truncate ${isSelected ? 'text-arena-cyan' : 'text-arena-text'}`}>
                  {team.name}
                </p>
                <p className="text-[10px] text-arena-textMuted uppercase tracking-wide">
                  {isSelected ? '✓ Selected' : 'Click to join'}
                </p>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
};

export default TeamSelector;
