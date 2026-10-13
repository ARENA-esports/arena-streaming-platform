import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Swords,
  RefreshCw,
  Flame,
  Coins,
  Trophy,
  AlertCircle,
  ChevronDown,
  Clock,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { analyticsService } from '../api/analyticsService';
import {
  StreamTeamBattleResponse,
  StreamRoundOutcomeResponse,
  StreamBattleDashboardResponse,
} from '../types';

export const formatLastAttackAt = (timestamp: string | null): string => {
  if (!timestamp) return 'No attacks yet';
  const date = new Date(timestamp);
  if (isNaN(date.getTime())) return 'No attacks yet';
  return date.toLocaleString();
};

export const formatCompletedAt = (timestamp: string | null): string => {
  if (!timestamp) return 'In progress';
  const date = new Date(timestamp);
  if (isNaN(date.getTime())) return 'In progress';
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
};

export const BattleStatsDashboardView: React.FC = () => {
  const [selectedStreamId, setSelectedStreamId] = useState<number | 'all'>('all');
  const [allSummaries, setAllSummaries] = useState<StreamTeamBattleResponse[]>([]);
  const [streamDashboard, setStreamDashboard] = useState<StreamBattleDashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Load battle summaries or stream-specific dashboard data
  const fetchData = useCallback(async (targetStreamId?: number | 'all') => {
    setIsLoading(true);
    setError(null);
    try {
      const summaries = await analyticsService.getAllBattleSummaries();
      const summaryList = Array.isArray(summaries) ? summaries : [];
      setAllSummaries(summaryList);

      let streamToUse = targetStreamId;
      if (streamToUse === undefined) {
        streamToUse = summaryList.length > 0 ? summaryList[0].streamId : 'all';
        setSelectedStreamId(streamToUse);
      }

      if (typeof streamToUse === 'number') {
        const dashboard = await analyticsService.getStreamBattleDashboard(streamToUse);
        setStreamDashboard(dashboard || null);
      } else {
        setStreamDashboard(null);
      }
    } catch (err) {
      setError('Failed to load battle analytics data. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleStreamChange = async (val: string) => {
    const streamId = val === 'all' ? 'all' : Number(val);
    setSelectedStreamId(streamId);
    if (streamId === 'all') {
      setStreamDashboard(null);
    } else {
      setIsLoading(true);
      setError(null);
      try {
        const dashboard = await analyticsService.getStreamBattleDashboard(streamId);
        setStreamDashboard(dashboard || null);
      } catch (err) {
        setError('Failed to load battle analytics data. Please try again.');
      } finally {
        setIsLoading(false);
      }
    }
  };

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Extract unique stream IDs across all known summaries for stream selector
  const availableStreamIds = useMemo(() => {
    const ids = new Set<number>();
    allSummaries.forEach((s) => {
      if (s.streamId) ids.add(s.streamId);
    });
    if (streamDashboard?.streamId) {
      ids.add(streamDashboard.streamId);
    }
    return Array.from(ids).sort((a, b) => a - b);
  }, [allSummaries, streamDashboard]);

  // Active teams list depending on selected view
  const activeTeams: StreamTeamBattleResponse[] = useMemo(() => {
    if (selectedStreamId === 'all') {
      return allSummaries;
    }
    return streamDashboard?.teams || [];
  }, [selectedStreamId, allSummaries, streamDashboard]);

  // Active round outcomes list
  const activeRounds: StreamRoundOutcomeResponse[] = useMemo(() => {
    if (selectedStreamId === 'all') {
      return [];
    }
    return streamDashboard?.rounds || [];
  }, [selectedStreamId, streamDashboard]);

  // Aggregated KPI calculations
  const totalAttacks = useMemo(
    () => activeTeams.reduce((sum, t) => sum + (t.totalAttacks || 0), 0),
    [activeTeams]
  );

  const totalDamage = useMemo(
    () => activeTeams.reduce((sum, t) => sum + (t.totalDamageDealt || 0), 0),
    [activeTeams]
  );

  const totalCoins = useMemo(
    () => activeTeams.reduce((sum, t) => sum + (t.totalCoinsSpent || 0), 0),
    [activeTeams]
  );

  const totalRoundsPlayed = useMemo(() => {
    if (selectedStreamId !== 'all') {
      return activeRounds.length;
    }
    return activeTeams.reduce((sum, t) => sum + (t.roundsWon || 0), 0);
  }, [selectedStreamId, activeRounds, activeTeams]);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 text-arena-text">
      {/* Top Analytics Tab Switcher */}
      <div className="flex items-center gap-3 mb-6 pb-2 border-b border-arena-border">
        <Link
          to="/organizer/analytics"
          className="px-4 py-2 text-xs font-bold font-mono tracking-wider uppercase rounded-[10px] text-arena-textMuted hover:text-white hover:bg-zinc-800/60 transition-all border border-transparent"
        >
          Viewer Engagement
        </Link>
        <span
          className="px-4 py-2 text-xs font-bold font-mono tracking-wider uppercase rounded-[10px] bg-arena-cyan/15 text-arena-cyan border border-arena-cyan/30 shadow-[0_0_10px_rgba(0,184,252,0.15)] cursor-default"
          aria-current="page"
        >
          Battle & Attack Stats
        </span>
      </div>

      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8 pb-4 border-b border-arena-border">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 shrink-0">
            <Swords size={26} />
          </div>
          <div>
            <h1 className="text-3xl font-bold text-white tracking-tight font-display">
              Battle & Attack Stats Dashboard
            </h1>
            <p className="text-sm text-arena-textMuted mt-1">
              Attack volume, damage dealt, and round outcomes per stream and team
            </p>
          </div>
        </div>

        {/* Action Controls: Stream Selector & Refresh Button */}
        <div className="flex items-center gap-3 shrink-0">
          <div className="relative">
            <select
              aria-label="Filter by stream"
              value={selectedStreamId}
              onChange={(e) => handleStreamChange(e.target.value)}
              className="bg-[var(--panel-2)] border border-arena-border hover:border-arena-cyan focus:border-arena-cyan text-white text-xs font-bold uppercase tracking-wider rounded-[14px] px-4 py-2.5 outline-none transition-colors cursor-pointer appearance-none pr-9 shadow-sm"
            >
              <option value="all" className="bg-zinc-900 text-white">
                All Streams ({availableStreamIds.length || 0})
              </option>
              {availableStreamIds.map((id) => (
                <option key={id} value={id} className="bg-zinc-900 text-white">
                  Stream {id}
                </option>
              ))}
            </select>
            <ChevronDown
              size={14}
              className="absolute right-3.5 top-1/2 -translate-y-1/2 text-arena-cyan pointer-events-none"
            />
          </div>

          <button
            onClick={() => fetchData(selectedStreamId)}
            disabled={isLoading}
            className="inline-flex items-center gap-2 bg-[var(--panel-2)] hover:bg-zinc-800 text-white border border-arena-border hover:border-arena-cyan font-bold py-2.5 px-4 rounded-[14px] transition-all text-xs shrink-0 cursor-pointer disabled:opacity-50"
            aria-label="Refresh battle stats"
          >
            <RefreshCw
              size={15}
              className={isLoading ? 'animate-spin text-arena-cyan' : 'text-arena-cyan'}
            />
            <span>Refresh</span>
          </button>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex flex-col items-center justify-center py-20" role="status" aria-live="polite">
          <div className="w-10 h-10 border-4 border-rose-500 border-t-transparent rounded-full animate-spin mb-4"></div>
          <p className="text-sm text-arena-textMuted font-mono">Loading battle analytics...</p>
        </div>
      )}

      {/* Error State */}
      {!isLoading && error && (
        <div className="bg-red-950/40 border border-red-500/40 rounded-[14px] p-8 text-center max-w-lg mx-auto my-12 shadow-lg">
          <div className="w-12 h-12 rounded-full bg-red-500/10 border border-red-500/30 flex items-center justify-center mx-auto mb-4 text-rose-400">
            <AlertCircle size={24} />
          </div>
          <h2 className="text-lg font-bold text-white mb-2">Unable to Load Battle Analytics</h2>
          <p className="text-sm text-red-200 mb-6">{error}</p>
          <button
            onClick={() => fetchData(selectedStreamId)}
            className="bg-rose-500 hover:bg-rose-600 text-white font-bold py-2.5 px-6 rounded-[14px] shadow-[0_0_15px_rgba(244,63,94,0.3)] transition-all text-xs uppercase tracking-wider cursor-pointer"
          >
            Retry
          </button>
        </div>
      )}

      {/* Main Content */}
      {!isLoading && !error && (
        <>
          {/* Summary KPI Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            {/* Card 1: Total Attacks */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-rose-500/30 transition-all">
              <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 shrink-0">
                <Swords size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Attack Volume
                </p>
                <p className="text-2xl font-bold text-white font-display mt-0.5" title="Total Attacks">
                  {totalAttacks.toLocaleString()} attacks
                </p>
              </div>
            </div>

            {/* Card 2: Total Damage */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-amber-500/30 transition-all">
              <div className="p-3 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 shrink-0">
                <Flame size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Total Damage Dealt
                </p>
                <p className="text-2xl font-bold text-amber-300 font-display mt-0.5" title="Total Damage">
                  {totalDamage.toLocaleString()} dmg
                </p>
              </div>
            </div>

            {/* Card 3: Coins Spent */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-yellow-500/30 transition-all">
              <div className="p-3 rounded-xl bg-yellow-500/10 border border-yellow-500/20 text-yellow-400 shrink-0">
                <Coins size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Coins Spent on Attacks
                </p>
                <p className="text-2xl font-bold text-yellow-300 font-display mt-0.5" title="Coins Spent">
                  {totalCoins.toLocaleString()} coins
                </p>
              </div>
            </div>

            {/* Card 4: Rounds */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-purple-500/30 transition-all">
              <div className="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-400 shrink-0">
                <Trophy size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  {selectedStreamId === 'all' ? 'Rounds Recorded' : 'Rounds Completed'}
                </p>
                <p className="text-2xl font-bold text-white font-display mt-0.5" title="Rounds">
                  {totalRoundsPlayed.toLocaleString()} {totalRoundsPlayed === 1 ? 'round' : 'rounds'}
                </p>
              </div>
            </div>
          </div>

          {/* Empty State when no team battle stats exist */}
          {activeTeams.length === 0 ? (
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-12 text-center shadow-lg my-6">
              <div className="w-16 h-16 rounded-2xl bg-zinc-800/80 border border-arena-border flex items-center justify-center mx-auto mb-4 text-arena-textMuted">
                <Swords size={32} />
              </div>
              <h2 className="text-xl font-bold text-white mb-2">No battle or attack data yet</h2>
              <p className="text-sm text-arena-textMuted max-w-md mx-auto">
                {selectedStreamId === 'all'
                  ? 'Attack metrics and round outcomes will appear here once viewers launch attacks on streams.'
                  : `Stream ${selectedStreamId} has no attack records yet.`}
              </p>
            </div>
          ) : (
            <>
              {/* Per-Team Battle Statistics Breakdown */}
              <div className="bg-arena-surface border border-arena-border rounded-[14px] overflow-hidden shadow-lg mb-8">
                <div className="px-6 py-4 border-b border-arena-border flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <h2 className="text-base font-bold text-white uppercase tracking-wider font-display">
                      Team Attack Volume & Performance
                    </h2>
                    {selectedStreamId !== 'all' && (
                      <span className="px-2.5 py-0.5 rounded-full text-[10px] font-mono font-bold bg-arena-cyan/15 text-arena-cyan border border-arena-cyan/30">
                        Stream {selectedStreamId}
                      </span>
                    )}
                  </div>
                  <span className="text-xs text-arena-textMuted font-mono">
                    {activeTeams.length} {activeTeams.length === 1 ? 'team' : 'teams'} participating
                  </span>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="border-b border-arena-border bg-[var(--panel-2)]/60 text-[11px] font-bold uppercase tracking-wider text-arena-textMuted">
                        {selectedStreamId === 'all' && <th className="py-3.5 px-6">Stream ID</th>}
                        <th className="py-3.5 px-6">Team</th>
                        <th className="py-3.5 px-6">Attack Volume</th>
                        <th className="py-3.5 px-6">Damage Dealt</th>
                        <th className="py-3.5 px-6">Coins Spent</th>
                        <th className="py-3.5 px-6">Round Record</th>
                        <th className="py-3.5 px-6">Last Attack</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-arena-border text-sm">
                      {activeTeams.map((team) => {
                        const attackPercent =
                          totalAttacks > 0
                            ? Math.round((team.totalAttacks / totalAttacks) * 100)
                            : 0;

                        return (
                          <tr
                            key={`${team.streamId}-${team.teamId}`}
                            className="hover:bg-zinc-800/40 transition-colors"
                          >
                            {/* Stream ID (shown when All Streams is selected) */}
                            {selectedStreamId === 'all' && (
                              <td className="py-4 px-6 font-mono text-xs text-arena-textMuted font-semibold">
                                Stream {team.streamId}
                              </td>
                            )}

                            {/* Team Name */}
                            <td className="py-4 px-6">
                              <div className="flex items-center gap-2.5">
                                <span className="w-2.5 h-2.5 rounded-full bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.6)] shrink-0" />
                                <div>
                                  <span className="font-bold text-white block">
                                    {team.teamName || `Team #${team.teamId}`}
                                  </span>
                                  <span className="text-[11px] text-arena-textMuted font-mono">
                                    ID: {team.teamId}
                                  </span>
                                </div>
                              </div>
                            </td>

                            {/* Attack Volume (with visual comparison bar) */}
                            <td className="py-4 px-6">
                              <div className="w-48">
                                <div className="flex items-center justify-between text-xs font-mono mb-1">
                                  <span className="font-bold text-white">
                                    {team.totalAttacks.toLocaleString()} attacks
                                  </span>
                                  <span className="text-rose-400 font-semibold">{attackPercent}%</span>
                                </div>
                                <div className="w-full bg-zinc-800 rounded-full h-2 overflow-hidden border border-zinc-700/50">
                                  <div
                                    className="bg-rose-500 h-2 rounded-full transition-all duration-500"
                                    style={{ width: `${Math.min(attackPercent, 100)}%` }}
                                  />
                                </div>
                              </div>
                            </td>

                            {/* Damage Dealt */}
                            <td className="py-4 px-6">
                              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] text-xs font-bold font-mono bg-amber-500/10 text-amber-300 border border-amber-500/25">
                                <Flame size={12} />
                                {team.totalDamageDealt.toLocaleString()} dmg
                              </span>
                            </td>

                            {/* Coins Spent */}
                            <td className="py-4 px-6 font-mono text-zinc-300 text-xs">
                              {team.totalCoinsSpent.toLocaleString()} coins
                            </td>

                            {/* Round Record (Won / Lost) */}
                            <td className="py-4 px-6">
                              <div className="inline-flex items-center gap-2 font-mono text-xs">
                                <span className="px-2 py-0.5 rounded-md bg-emerald-500/15 text-emerald-400 font-bold border border-emerald-500/30">
                                  {team.roundsWon} W
                                </span>
                                <span className="text-zinc-500">-</span>
                                <span className="px-2 py-0.5 rounded-md bg-zinc-800 text-zinc-400 font-bold border border-zinc-700">
                                  {team.roundsLost} L
                                </span>
                              </div>
                            </td>

                            {/* Last Attack Timestamp */}
                            <td className="py-4 px-6 text-xs text-arena-textMuted font-mono">
                              {formatLastAttackAt(team.lastAttackAt)}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Round Outcomes History Section */}
              <div className="bg-arena-surface border border-arena-border rounded-[14px] overflow-hidden shadow-lg">
                <div className="px-6 py-4 border-b border-arena-border flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <h2 className="text-base font-bold text-white uppercase tracking-wider font-display">
                      Round Outcome History
                    </h2>
                    {selectedStreamId !== 'all' && (
                      <span className="text-xs text-arena-textMuted font-mono">
                        ({activeRounds.length} rounds)
                      </span>
                    )}
                  </div>
                  {selectedStreamId === 'all' && (
                    <span className="text-xs text-arena-cyan font-mono">
                      Select a specific stream above to view round-by-round progression
                    </span>
                  )}
                </div>

                {selectedStreamId === 'all' ? (
                  <div className="p-8 text-center text-arena-textMuted font-mono text-xs">
                    Please choose a stream from the dropdown above to inspect detailed round outcomes.
                  </div>
                ) : activeRounds.length === 0 ? (
                  <div className="p-8 text-center text-arena-textMuted font-mono text-xs">
                    No completed rounds recorded for Stream {selectedStreamId}.
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                      <thead>
                        <tr className="border-b border-arena-border bg-[var(--panel-2)]/60 text-[11px] font-bold uppercase tracking-wider text-arena-textMuted">
                          <th className="py-3.5 px-6">Round</th>
                          <th className="py-3.5 px-6">Winner</th>
                          <th className="py-3.5 px-6">Team A Attacks & Dmg</th>
                          <th className="py-3.5 px-6">Team B Attacks & Dmg</th>
                          <th className="py-3.5 px-6">Completed At</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-arena-border text-sm">
                        {activeRounds.map((round) => (
                          <tr
                            key={`${round.streamId}-${round.roundNumber}`}
                            className="hover:bg-zinc-800/40 transition-colors"
                          >
                            {/* Round Number */}
                            <td className="py-4 px-6 font-mono font-bold text-white">
                              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-[8px] bg-zinc-800 border border-zinc-700 text-xs">
                                Round {round.roundNumber}
                              </span>
                            </td>

                            {/* Winning Team */}
                            <td className="py-4 px-6">
                              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] text-xs font-bold font-mono bg-emerald-500/10 text-emerald-300 border border-emerald-500/25">
                                <Trophy size={13} className="text-emerald-400" />
                                {round.winningTeamName || `Team #${round.winningTeamId}`}
                              </span>
                            </td>

                            {/* Team A Metrics */}
                            <td className="py-4 px-6 font-mono text-xs">
                              <span className="text-zinc-300 font-semibold">
                                {round.teamAAttacks} attacks
                              </span>
                              <span className="text-arena-textMuted ml-1.5">
                                ({round.teamADamage} dmg)
                              </span>
                            </td>

                            {/* Team B Metrics */}
                            <td className="py-4 px-6 font-mono text-xs">
                              <span className="text-zinc-300 font-semibold">
                                {round.teamBAttacks} attacks
                              </span>
                              <span className="text-arena-textMuted ml-1.5">
                                ({round.teamBDamage} dmg)
                              </span>
                            </td>

                            {/* Completed Timestamp */}
                            <td className="py-4 px-6 text-xs text-arena-textMuted font-mono">
                              <span className="inline-flex items-center gap-1">
                                <Clock size={12} />
                                {formatCompletedAt(round.completedAt)}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}
        </>
      )}
    </div>
  );
};

export default BattleStatsDashboardView;
