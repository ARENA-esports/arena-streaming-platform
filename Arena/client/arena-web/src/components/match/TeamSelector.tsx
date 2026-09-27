import React, { useState, useEffect } from 'react';
import { Shield, Check } from 'lucide-react';
import { teamService, Team, formatLogoUrl } from '../../api/teamService';
import { matchService } from '../../api/matchService';

interface TeamSelectorProps {
  matchId: number | string;
  selectedTeamId?: number | null;
  onTeamSelect?: (teamId: number | null) => void;
  onTeamsLoaded?: (teamA: Team, teamB: Team) => void;
}

export const TeamSelector: React.FC<TeamSelectorProps> = ({
  matchId,
  selectedTeamId: propSelectedTeamId,
  onTeamSelect,
  onTeamsLoaded,
}) => {
  const [teamA, setTeamA] = useState<Team | null>(null);
  const [teamB, setTeamB] = useState<Team | null>(null);
  const [internalSelectedId, setInternalSelectedId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [imageErrors, setImageErrors] = useState<Record<number, boolean>>({});

  const activeSelectedId = propSelectedTeamId !== undefined ? propSelectedTeamId : internalSelectedId;

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
          onTeamsLoaded?.(a, b);
        }
      } catch (err) {
        console.error('[TeamSelector] Failed to load teams', err);
      } finally {
        if (isMounted) setLoading(false);
      }
    }
    fetchTeams();
    return () => {
      isMounted = false;
    };
  }, [matchId, onTeamsLoaded]);

  const handleSelect = (teamId: number) => {
    const newId = activeSelectedId === teamId ? null : teamId;
    setInternalSelectedId(newId);
    onTeamSelect?.(newId);
  };

  const handleImageError = (teamId: number) => {
    setImageErrors((prev) => ({ ...prev, [teamId]: true }));
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
      <div className="flex items-center justify-between px-4 py-2.5 border-b border-arena-border bg-arena-surface/80">
        <div className="flex items-center gap-2">
          <Shield size={14} className="text-arena-cyan" />
          <span className="text-xs font-bold uppercase tracking-wider text-arena-text">
            Choose Your Faction
          </span>
        </div>
        <span className="text-[10px] text-arena-textMuted uppercase font-mono">
          Pick side to chat
        </span>
      </div>

      {/* Team cards - Full-width vertical stack so both teams are always clearly visible */}
      <div className="flex flex-col gap-2 p-3">
        {[teamA, teamB].map((team) => {
          const isSelected = activeSelectedId === team.teamId;
          const hasImageError = imageErrors[team.teamId];
          const logoSrc = team.logoUrl && !hasImageError ? formatLogoUrl(team.logoUrl) : null;
          const teamColor = team.colorHex || '#00B8FC';
          const initial = team.name.charAt(0).toUpperCase();

          return (
            <button
              key={team.teamId}
              type="button"
              onClick={() => handleSelect(team.teamId)}
              className={`
                w-full flex items-center gap-3 p-3 rounded-xl border-2 transition-all duration-200 cursor-pointer text-left
                ${
                  isSelected
                    ? 'shadow-md'
                    : 'border-arena-border hover:border-arena-textMuted bg-arena-bg/40 hover:bg-arena-surfaceHover/50'
                }
              `}
              style={
                isSelected
                  ? {
                      borderColor: teamColor,
                      backgroundColor: `${teamColor}12`,
                      boxShadow: `0 0 16px -4px ${teamColor}40`,
                    }
                  : undefined
              }
              id={`team-select-${team.teamId}`}
            >
              {/* Team emblem / logo */}
              <div
                className="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 overflow-hidden shadow-sm font-black text-sm text-white"
                style={{ backgroundColor: teamColor }}
              >
                {logoSrc ? (
                  <img
                    src={logoSrc}
                    alt={team.name}
                    onError={() => handleImageError(team.teamId)}
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <span>{initial}</span>
                )}
              </div>

              {/* Team info */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between gap-1">
                  <p
                    className="text-sm font-bold truncate tracking-wide"
                    style={{ color: isSelected ? teamColor : undefined }}
                  >
                    {team.name}
                  </p>
                  {isSelected && (
                    <span
                      className="flex items-center gap-1 text-[10px] font-extrabold uppercase px-1.5 py-0.5 rounded shrink-0"
                      style={{
                        color: teamColor,
                        backgroundColor: `${teamColor}20`,
                      }}
                    >
                      <Check size={10} strokeWidth={3} />
                      Active
                    </span>
                  )}
                </div>
                <p className="text-[11px] text-arena-textMuted mt-0.5">
                  {isSelected ? 'You are representing this faction' : 'Click to support in chat'}
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
