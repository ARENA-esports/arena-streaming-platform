import React, { useEffect, useState } from 'react';
import { X, Trophy, History, Clock, ShieldAlert } from 'lucide-react';
import { weaponShopService } from '../../api/weaponShopService';
import { BattleRoundHistoryDto } from '../../types';

interface RoundHistoryModalProps {
  matchId: number | string;
  isOpen: boolean;
  onClose: () => void;
  teamsMap?: Record<number, { name: string; color: string }>;
}

export const RoundHistoryModal: React.FC<RoundHistoryModalProps> = ({
  matchId,
  isOpen,
  onClose,
  teamsMap,
}) => {
  const [history, setHistory] = useState<BattleRoundHistoryDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    let isMounted = true;
    setIsLoading(true);
    setError(null);

    weaponShopService
      .getRoundHistory(Number(matchId))
      .then((data) => {
        if (isMounted) {
          setHistory(data);
          setIsLoading(false);
        }
      })
      .catch((err) => {
        if (isMounted) {
          console.error('Failed to load round history:', err);
          setError('Failed to load round history. Please try again.');
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [matchId, isOpen]);

  // Handle escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fade-in"
      onClick={onClose}
      data-testid="round-history-modal"
    >
      <div
        className="w-full max-w-xl bg-arena-surface border border-arena-border rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[85vh]"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-arena-border bg-arena-card">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-arena-cyan/10 border border-arena-cyan/30 text-arena-cyan">
              <History size={20} />
            </div>
            <div>
              <h2 className="text-lg font-black text-white uppercase tracking-wider">
                Battle Round History
              </h2>
              <p className="text-xs text-arena-textMuted font-mono">
                Match #{matchId} Past Round Outcomes
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-arena-textMuted hover:text-white hover:bg-arena-surface transition-colors"
            aria-label="Close"
          >
            <X size={20} />
          </button>
        </div>

        {/* Modal Content */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1">
          {isLoading && (
            <div className="py-12 flex flex-col items-center justify-center gap-3 text-arena-textMuted">
              <div className="w-8 h-8 rounded-full border-2 border-arena-cyan border-t-transparent animate-spin" />
              <span className="text-sm font-mono">Loading round log...</span>
            </div>
          )}

          {error && (
            <div className="p-4 rounded-xl bg-arena-crimson/10 border border-arena-crimson/30 flex items-center gap-3 text-arena-crimson">
              <ShieldAlert size={20} />
              <span className="text-sm">{error}</span>
            </div>
          )}

          {!isLoading && !error && history.length === 0 && (
            <div className="py-12 px-4 text-center flex flex-col items-center justify-center">
              <div className="w-14 h-14 rounded-2xl bg-arena-card border border-arena-border flex items-center justify-center text-arena-textMuted mb-3">
                <Trophy size={26} className="opacity-50" />
              </div>
              <h3 className="text-base font-bold text-white mb-1">No Completed Rounds Yet</h3>
              <p className="text-xs text-arena-textMuted max-w-sm">
                Round 1 is currently in progress! As teams hit 100% damage, each round's final outcome and damage breakdown will appear here.
              </p>
            </div>
          )}

          {!isLoading &&
            !error &&
            history.length > 0 &&
            history.map((round) => {
              const winnerName = round.winningTeamId
                ? teamsMap?.[round.winningTeamId]?.name || `Team ${round.winningTeamId}`
                : 'Undecided';
              const winnerColor = round.winningTeamId
                ? teamsMap?.[round.winningTeamId]?.color || '#00B8FC'
                : '#A1A1AA';

              return (
                <div
                  key={round.roundId}
                  className="p-4 rounded-xl bg-arena-card border border-arena-border hover:border-arena-borderFocus transition-colors shadow-sm"
                  data-testid={`round-history-item-${round.roundNumber}`}
                >
                  {/* Round Header */}
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-2">
                      <span className="px-2.5 py-0.5 rounded text-xs font-mono font-black uppercase tracking-wider bg-arena-surface border border-arena-border text-arena-cyan">
                        Round {round.roundNumber}
                      </span>
                      {round.endedAt && (
                        <span className="flex items-center gap-1 text-[11px] text-arena-textMuted font-mono">
                          <Clock size={12} />
                          {new Date(round.endedAt).toLocaleTimeString([], {
                            hour: '2-digit',
                            minute: '2-digit',
                            second: '2-digit',
                          })}
                        </span>
                      )}
                    </div>

                    {/* Winner Pill */}
                    <div
                      className="flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold font-mono"
                      style={{
                        backgroundColor: `${winnerColor}18`,
                        border: `1px solid ${winnerColor}40`,
                        color: winnerColor,
                      }}
                    >
                      <Trophy size={13} />
                      <span>Winner: {winnerName}</span>
                    </div>
                  </div>

                  {/* Final Bar State Breakdown */}
                  {round.finalBarState && round.finalBarState.length > 0 && (
                    <div className="space-y-1.5 pt-1 border-t border-arena-border/50">
                      <div className="text-[11px] uppercase font-mono tracking-wider text-arena-textMuted mb-1">
                        Final Bar State
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        {round.finalBarState.map((bar) => {
                          const teamMeta = teamsMap?.[bar.teamId];
                          const teamName = teamMeta?.name || `Team ${bar.teamId}`;
                          const teamColor = teamMeta?.color || (bar.teamId === 1 ? '#EF4444' : '#00B8FC');
                          const isWinner = bar.teamId === round.winningTeamId;

                          return (
                            <div
                              key={bar.teamId}
                              className={`p-2.5 rounded-lg bg-arena-surface/80 border ${
                                isWinner ? 'border-amber-500/30' : 'border-arena-border'
                              }`}
                            >
                              <div className="flex items-center justify-between text-xs mb-1">
                                <span className="font-bold truncate max-w-[120px]" style={{ color: teamColor }}>
                                  {teamName}
                                </span>
                                <span className="font-mono font-black text-white">
                                  {bar.totalDamage.toLocaleString()} DMG
                                </span>
                              </div>
                              <div className="w-full h-1.5 rounded-full bg-arena-border overflow-hidden">
                                <div
                                  className="h-full rounded-full transition-all duration-300"
                                  style={{
                                    width: `${Math.min(100, Math.round((bar.totalDamage / round.targetDamage) * 100))}%`,
                                    backgroundColor: teamColor,
                                  }}
                                />
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
        </div>

        {/* Modal Footer */}
        <div className="px-6 py-3 border-t border-arena-border bg-arena-card flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 rounded-xl text-xs font-bold text-arena-textMuted hover:text-white bg-arena-surface hover:bg-arena-surface/80 border border-arena-border transition-colors uppercase tracking-wider"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default RoundHistoryModal;
