import { FC, useEffect, useState } from 'react';
import { MatchScheduleResponse } from '../types';
import { apiClient } from '../api/client';
import { useSearchParams } from 'react-router-dom';
import MatchList from '../components/match/MatchList';
import { ChevronDown } from 'lucide-react';

export const MatchesView: FC = () => {
  const [matches, setMatches] = useState<MatchScheduleResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchParams] = useSearchParams();
  const teamIdParam = searchParams.get('teamId');

  useEffect(() => {
    const fetchMatches = async () => {
      try {
        const baseParams = teamIdParam ? { teamId: teamIdParam } : {};
        
        const [liveRes, scheduledRes, pastRes] = await Promise.all([
          apiClient.get<MatchScheduleResponse[]>('/matches', { params: { ...baseParams, status: 'Live' } }),
          apiClient.get<MatchScheduleResponse[]>('/matches', { params: { ...baseParams, status: 'Scheduled' } }),
          apiClient.get<MatchScheduleResponse[]>('/matches', { params: { ...baseParams, status: 'Ended' } })
        ]);
        
        setMatches([...liveRes.data, ...scheduledRes.data, ...pastRes.data]);
      } catch (err) {
        console.error('Failed to fetch matches', err);
      } finally {
        setIsLoading(false);
      }
    };
    fetchMatches();
  }, [teamIdParam]);

  const [activeFilter, setActiveFilter] = useState<'all' | 'live' | 'upcoming' | 'completed'>('all');

  const liveMatches = matches.filter((m) => m.status === 'Live');
  const upcomingMatches = matches.filter((m) => m.status === 'Scheduled');
  const pastMatches = matches.filter((m) => m.status === 'Ended');

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Category Header & Filter Dropdown */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8 pb-4 border-b border-arena-border">
        <div>
          <h1 className="text-3xl font-bold text-white tracking-tight font-display">Esports Matches</h1>
          <p className="text-sm text-arena-textMuted mt-1">Browse live broadcasts, upcoming schedules, and recent match replays</p>
        </div>
        <div className="relative shrink-0 w-full sm:w-auto">
          <select
            value={activeFilter}
            onChange={(e) => setActiveFilter(e.target.value as any)}
            className="w-full sm:w-56 bg-[var(--panel-2)] border border-arena-border hover:border-arena-cyan focus:border-arena-cyan text-white text-xs font-bold uppercase tracking-wider rounded-[14px] px-4 py-2.5 outline-none transition-colors cursor-pointer appearance-none pr-9 shadow-sm"
          >
            <option value="all" className="bg-zinc-900 text-white">All Matches ({matches.length})</option>
            <option value="live" className="bg-zinc-900 text-white">Live Now ({liveMatches.length})</option>
            <option value="upcoming" className="bg-zinc-900 text-white">Upcoming ({upcomingMatches.length})</option>
            <option value="completed" className="bg-zinc-900 text-white">Completed ({pastMatches.length})</option>
          </select>
          <ChevronDown size={14} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-arena-cyan pointer-events-none" />
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20">
          <div className="w-8 h-8 border-4 border-[#00B8FC] border-t-transparent rounded-full animate-spin"></div>
        </div>
      ) : (
        <>
          {(activeFilter === 'all' || activeFilter === 'live') && liveMatches.length > 0 && (
            <MatchList title="Live Now" matches={liveMatches} />
          )}
          {(activeFilter === 'all' || activeFilter === 'upcoming') && (
            <MatchList title="Upcoming Matches" matches={upcomingMatches} />
          )}
          {(activeFilter === 'all' || activeFilter === 'completed') && pastMatches.length > 0 && (
            <MatchList title="Completed Matches" matches={pastMatches} />
          )}
        </>
      )}
    </div>
  );
};

export default MatchesView;
