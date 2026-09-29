import React, { useEffect, useState } from 'react';
import { Check, Shield } from 'lucide-react';

import { matchService } from '../../api/matchService';
import { formatLogoUrl, teamService, Team } from '../../api/teamService';

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

      <div className="grid grid-cols-2 gap-2.5 p-2.5 sm:p-3">
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
                flex items-center gap-2.5 p-2.5 rounded-xl border-2 transition-all duration-200 cursor-pointer text-left min-w-0
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
                      backgroundColor: `${teamColor}15`,
                      boxShadow: `0 0 16px -4px ${teamColor}40`,
                    }
                  : undefined
              }
              id={`team-select-${team.teamId}`}
              title={team.name}
            >
              <div
                className="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0 overflow-hidden shadow-sm font-black text-sm text-white"
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

              <div className="flex-1 min-w-0">
                <p
                  className="text-xs sm:text-sm font-bold truncate tracking-wide"
                  style={{ color: isSelected ? teamColor : undefined }}
                >
                  {team.name}
                </p>
                <div className="mt-0.5 flex items-center">
                  {isSelected ? (
                    <span
                      className="inline-flex items-center gap-1 text-[9px] font-extrabold uppercase px-1.5 py-0.5 rounded"
                      style={{
                        color: teamColor,
                        backgroundColor: `${teamColor}25`,
                      }}
                    >
                      <Check size={9} strokeWidth={3} />
                      Active
                    </span>
                  ) : (
                    <span className="text-[10px] text-arena-textMuted truncate">
                      Select
                    </span>
                  )}
                </div>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
};

export default TeamSelector;
