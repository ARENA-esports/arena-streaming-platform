import React, { useState, useEffect, useCallback } from 'react';
import { BarChart2, RefreshCw, Clock, Coins, Radio, Users, AlertCircle } from 'lucide-react';
import { analyticsService } from '../api/analyticsService';
import { StreamEngagementResponse } from '../types';

export const formatWatchTime = (totalSeconds: number): string => {
  if (!totalSeconds || totalSeconds <= 0) return '0s';
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = Math.floor(totalSeconds % 60);

  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }
  if (minutes > 0) {
    return `${minutes}m ${seconds}s`;
  }
  return `${seconds}s`;
};

export const formatLastEventAt = (timestamp: string | null): string => {
  if (!timestamp) return 'No activity';
  const date = new Date(timestamp);
  if (isNaN(date.getTime())) return 'No activity';
  return date.toLocaleString();
};

export const ViewerEngagementDashboardView: React.FC = () => {
  const [data, setData] = useState<StreamEngagementResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchEngagement = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await analyticsService.getStreamEngagement();
      setData(Array.isArray(response) ? response : []);
    } catch (err) {
      setError('Failed to load viewer engagement analytics. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchEngagement();
  }, [fetchEngagement]);

  // Aggregate KPI Calculations
  const totalWatchSeconds = data.reduce((sum, item) => sum + (item.totalWatchSeconds || 0), 0);
  const totalCoinsEarned = data.reduce((sum, item) => sum + (item.totalCoinsEarned || 0), 0);
  const trackedStreamsCount = data.length;
  const totalStreamViewers = data.reduce((sum, item) => sum + (item.uniqueViewers || 0), 0);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 text-arena-text">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8 pb-4 border-b border-arena-border">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-arena-cyan/10 border border-arena-cyan/20 text-arena-cyan shrink-0">
            <BarChart2 size={26} />
          </div>
          <div>
            <h1 className="text-3xl font-bold text-white tracking-tight font-display">
              Viewer Engagement Dashboard
            </h1>
            <p className="text-sm text-arena-textMuted mt-1">
              Viewer watch activity and coins earned per stream
            </p>
          </div>
        </div>
        <button
          onClick={fetchEngagement}
          disabled={isLoading}
          className="inline-flex items-center gap-2 bg-[var(--panel-2)] hover:bg-zinc-800 text-white border border-arena-border hover:border-arena-cyan font-bold py-2.5 px-4 rounded-[14px] transition-all text-xs shrink-0 cursor-pointer disabled:opacity-50"
          aria-label="Refresh engagement data"
        >
          <RefreshCw size={15} className={isLoading ? 'animate-spin text-arena-cyan' : 'text-arena-cyan'} />
          <span>Refresh</span>
        </button>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex flex-col items-center justify-center py-20" role="status" aria-live="polite">
          <div className="w-10 h-10 border-4 border-arena-cyan border-t-transparent rounded-full animate-spin mb-4"></div>
          <p className="text-sm text-arena-textMuted font-mono">Loading engagement analytics...</p>
        </div>
      )}

      {/* Error State */}
      {!isLoading && error && (
        <div className="bg-red-950/40 border border-red-500/40 rounded-[14px] p-8 text-center max-w-lg mx-auto my-12 shadow-lg">
          <div className="w-12 h-12 rounded-full bg-red-500/10 border border-red-500/30 flex items-center justify-center mx-auto mb-4 text-rose-400">
            <AlertCircle size={24} />
          </div>
          <h2 className="text-lg font-bold text-white mb-2">Unable to Load Analytics</h2>
          <p className="text-sm text-red-200 mb-6">{error}</p>
          <button
            onClick={fetchEngagement}
            className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2.5 px-6 rounded-[14px] shadow-[0_0_15px_rgba(0,184,252,0.3)] transition-all text-xs uppercase tracking-wider cursor-pointer"
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
            {/* Card 1: Total Watch Time */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-arena-cyan/30 transition-all">
              <div className="p-3 rounded-xl bg-blue-500/10 border border-blue-500/20 text-blue-400 shrink-0">
                <Clock size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Total Watch Time
                </p>
                <p className="text-2xl font-bold text-white font-display mt-0.5" title="Total Watch Time">
                  {formatWatchTime(totalWatchSeconds)}
                </p>
              </div>
            </div>

            {/* Card 2: Total Coins Earned */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-amber-500/30 transition-all">
              <div className="p-3 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 shrink-0">
                <Coins size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Total Coins Earned
                </p>
                <p className="text-2xl font-bold text-amber-300 font-display mt-0.5" title="Total Coins Earned">
                  {totalCoinsEarned.toLocaleString()}
                </p>
              </div>
            </div>

            {/* Card 3: Tracked Streams */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-arena-cyan/30 transition-all">
              <div className="p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 shrink-0">
                <Radio size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Tracked Streams
                </p>
                <p className="text-2xl font-bold text-white font-display mt-0.5" title="Tracked Streams">
                  {trackedStreamsCount.toLocaleString()}
                </p>
              </div>
            </div>

            {/* Card 4: Total Stream Viewers */}
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-5 shadow-md flex items-center gap-4 hover:border-purple-500/30 transition-all">
              <div className="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-400 shrink-0">
                <Users size={24} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-arena-textMuted font-semibold font-mono">
                  Total Stream Viewers
                </p>
                <p className="text-2xl font-bold text-white font-display mt-0.5" title="Total Stream Viewers">
                  {totalStreamViewers.toLocaleString()}
                </p>
              </div>
            </div>
          </div>

          {/* Empty State */}
          {data.length === 0 ? (
            <div className="bg-arena-surface border border-arena-border rounded-[14px] p-12 text-center shadow-lg my-6">
              <div className="w-16 h-16 rounded-2xl bg-zinc-800/80 border border-arena-border flex items-center justify-center mx-auto mb-4 text-arena-textMuted">
                <BarChart2 size={32} />
              </div>
              <h2 className="text-xl font-bold text-white mb-2">No viewer engagement data yet</h2>
              <p className="text-sm text-arena-textMuted max-w-md mx-auto">
                Engagement metrics will appear here once streams receive viewer activity.
              </p>
            </div>
          ) : (
            /* Main Per-Stream Engagement Table */
            <div className="bg-arena-surface border border-arena-border rounded-[14px] overflow-hidden shadow-lg">
              <div className="px-6 py-4 border-b border-arena-border flex items-center justify-between">
                <h2 className="text-base font-bold text-white uppercase tracking-wider font-display">
                  Per-Stream Engagement Metrics
                </h2>
                <span className="text-xs text-arena-textMuted font-mono">
                  {data.length} {data.length === 1 ? 'stream' : 'streams'} tracked
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-arena-border bg-[var(--panel-2)]/60 text-[11px] font-bold uppercase tracking-wider text-arena-textMuted">
                      <th className="py-3.5 px-6">Stream / Match ID</th>
                      <th className="py-3.5 px-6">Watch Time</th>
                      <th className="py-3.5 px-6">Coins Earned</th>
                      <th className="py-3.5 px-6">Watch Ticks</th>
                      <th className="py-3.5 px-6">Unique Viewers</th>
                      <th className="py-3.5 px-6">Last Activity</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-arena-border text-sm">
                    {data.map((stream) => (
                      <tr
                        key={stream.streamId}
                        className="hover:bg-zinc-800/40 transition-colors"
                      >
                        {/* Stream / Match ID */}
                        <td className="py-4 px-6 font-mono text-white font-semibold">
                          <div className="flex items-center gap-2">
                            <span className="w-2 h-2 rounded-full bg-arena-cyan/70"></span>
                            <span>Stream {stream.streamId}</span>
                          </div>
                        </td>

                        {/* Watch Time (Prominent) */}
                        <td className="py-4 px-6">
                          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] text-xs font-bold font-mono bg-blue-500/10 text-blue-400 border border-blue-500/25">
                            <Clock size={12} />
                            {formatWatchTime(stream.totalWatchSeconds)}
                          </span>
                        </td>

                        {/* Coins Earned (Prominent) */}
                        <td className="py-4 px-6">
                          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] text-xs font-bold font-mono bg-amber-500/10 text-amber-300 border border-amber-500/25">
                            <Coins size={12} />
                            {stream.totalCoinsEarned.toLocaleString()} coins
                          </span>
                        </td>

                        {/* Watch Ticks */}
                        <td className="py-4 px-6 font-mono text-zinc-300">
                          {stream.totalWatchTicks.toLocaleString()} ticks
                        </td>

                        {/* Unique Viewers */}
                        <td className="py-4 px-6 font-mono text-zinc-300">
                          {stream.uniqueViewers.toLocaleString()} viewers
                        </td>

                        {/* Last Activity */}
                        <td className="py-4 px-6 text-xs text-arena-textMuted font-mono">
                          {formatLastEventAt(stream.lastEventAt)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default ViewerEngagementDashboardView;
