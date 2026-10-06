import React, { useMemo } from 'react';
import { Swords, Flame } from 'lucide-react';
import { useBattleBar, BattleBarConnectionStatus } from '../../hooks/useBattleBar';

interface BattleBarProps {
  matchId: number | string;
  teamAId?: number | null;
  teamBId?: number | null;
  teamsMap?: Record<number, { name: string; color: string }>;
}

const ConnectionBadge: React.FC<{ status: BattleBarConnectionStatus }> = ({ status }) => {
  switch (status) {
    case 'connected':
      return (
        <span className="flex items-center gap-1 text-[11px] font-mono text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 px-2 py-0.5 rounded-full">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
          Live Battle Sync
        </span>
      );
    case 'connecting':
      return (
        <span className="flex items-center gap-1 text-[11px] font-mono text-amber-400 bg-amber-500/10 border border-amber-500/20 px-2 py-0.5 rounded-full">
          <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-ping" />
          Connecting...
        </span>
      );
    default:
      return (
        <span className="flex items-center gap-1 text-[11px] font-mono text-zinc-400 bg-zinc-500/10 border border-zinc-500/20 px-2 py-0.5 rounded-full">
          <span className="w-1.5 h-1.5 rounded-full bg-zinc-500" />
          Offline
        </span>
      );
  }
};

export const BattleBar: React.FC<BattleBarProps> = ({
  matchId,
  teamAId,
  teamBId,
  teamsMap,
}) => {
  const {
    bars,
    latestAttack,
    connectionStatus,
  } = useBattleBar({ matchId, teamAId, teamBId });

  // Resolve team metadata with safe defaults
  const teamA = useMemo(() => {
    const id = teamAId ?? 1;
    return {
      id,
      name: teamsMap?.[id]?.name || `Team ${id}`,
      color: teamsMap?.[id]?.color || '#EF4444', // Default Crimson
    };
  }, [teamAId, teamsMap]);

  const teamB = useMemo(() => {
    const id = teamBId ?? 2;
    return {
      id,
      name: teamsMap?.[id]?.name || `Team ${id}`,
      color: teamsMap?.[id]?.color || '#00B8FC', // Default Cyan
    };
  }, [teamBId, teamsMap]);

  const damageA = bars[teamA.id] ?? 0;
  const damageB = bars[teamB.id] ?? 0;
  const totalDamage = damageA + damageB;

  // Calculate percentages (50/50 baseline if no damage yet)
  const percentA = totalDamage > 0 ? Math.round((damageA / totalDamage) * 100) : 50;
  const percentB = 100 - percentA;

  return (
    <div
      data-testid="battle-bar"
      className="w-full bg-[var(--panel,#121722)] border border-[var(--line,#1E2638)] rounded-xl p-4 shadow-xl backdrop-blur-md relative overflow-hidden"
    >
      {/* Top Banner: Header, latest attack splash, and live connection status */}
      <div className="flex items-center justify-between gap-2 mb-3">
        <div className="flex items-center gap-2 min-w-0">
          <div className="p-1.5 rounded-lg bg-arena-crimson/10 border border-arena-crimson/30 text-arena-crimson">
            <Swords size={16} />
          </div>
          <span className="text-xs font-black uppercase tracking-wider text-arena-text">
            Faction Tug-of-War
          </span>
        </div>

        {/* Floating Attack Impact Banner */}
        {latestAttack && (
          <div className="animate-bounce flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-500/20 border border-amber-500/40 text-amber-300 text-xs font-bold shadow-lg">
            <Flame size={14} className="text-orange-400" />
            <span>
              {latestAttack.weaponName}: +{latestAttack.damage} DMG to{' '}
              {latestAttack.teamId === teamA.id ? teamA.name : teamB.name}!
            </span>
          </div>
        )}

        <ConnectionBadge status={connectionStatus} />
      </div>

      {/* Team Details & Damage Counters */}
      <div className="flex items-center justify-between mb-2 text-xs sm:text-sm">
        {/* Team A (Left) */}
        <div className="flex items-center gap-2">
          <span
            className="w-3 h-3 rounded-full shrink-0 shadow-sm"
            style={{ backgroundColor: teamA.color }}
          />
          <span className="font-bold text-white tracking-wide truncate max-w-[140px] sm:max-w-[200px]">
            {teamA.name}
          </span>
          <span
            className="font-mono font-black text-xs px-2 py-0.5 rounded"
            style={{
              color: teamA.color,
              backgroundColor: `${teamA.color}18`,
              border: `1px solid ${teamA.color}35`,
            }}
          >
            {damageA.toLocaleString()} DMG ({percentA}%)
          </span>
        </div>

        {/* Team B (Right) */}
        <div className="flex items-center gap-2">
          <span
            className="font-mono font-black text-xs px-2 py-0.5 rounded"
            style={{
              color: teamB.color,
              backgroundColor: `${teamB.color}18`,
              border: `1px solid ${teamB.color}35`,
            }}
          >
            ({percentB}%) {damageB.toLocaleString()} DMG
          </span>
          <span className="font-bold text-white tracking-wide truncate max-w-[140px] sm:max-w-[200px]">
            {teamB.name}
          </span>
          <span
            className="w-3 h-3 rounded-full shrink-0 shadow-sm"
            style={{ backgroundColor: teamB.color }}
          />
        </div>
      </div>

      {/* Dynamic Animated Tug-of-War Bar */}
      <div className="w-full h-5 bg-zinc-900/90 rounded-full p-1 border border-zinc-800 flex items-center relative overflow-hidden shadow-inner">
        {/* Team A Bar Segment */}
        <div
          className="h-full rounded-l-full transition-all duration-700 ease-out relative group"
          style={{
            width: `${percentA}%`,
            background: `linear-gradient(90deg, ${teamA.color}90, ${teamA.color})`,
            boxShadow: `0 0 12px ${teamA.color}50`,
          }}
        >
          <div className="absolute inset-0 bg-white/10 opacity-0 group-hover:opacity-100 transition-opacity" />
        </div>

        {/* Center Clash Marker */}
        <div className="absolute left-1/2 -translate-x-1/2 top-0 bottom-0 w-0.5 bg-white/40 z-10 pointer-events-none" />

        {/* Team B Bar Segment */}
        <div
          className="h-full rounded-r-full transition-all duration-700 ease-out relative group"
          style={{
            width: `${percentB}%`,
            background: `linear-gradient(90deg, ${teamB.color}, ${teamB.color}90)`,
            boxShadow: `0 0 12px ${teamB.color}50`,
          }}
        >
          <div className="absolute inset-0 bg-white/10 opacity-0 group-hover:opacity-100 transition-opacity" />
        </div>
      </div>

      {/* Sub-label */}
      <div className="flex justify-between items-center mt-2 text-[10px] text-arena-textMuted font-mono">
        <span>Team Attack Power</span>
        <span>
          Total Match Damage: <strong className="text-white">{totalDamage.toLocaleString()}</strong>
        </span>
        <span>Opposing Power</span>
      </div>
    </div>
  );
};

export default BattleBar;
