import { FC, useEffect, useState } from 'react';
import { MatchResponse } from '../types';
import { apiClient } from '../api/client';
import MatchList from '../components/match/MatchList';

export const MatchesView: FC = () => {
  const [matches, setMatches] = useState<MatchResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchMatches = async () => {
      try {
        const res = await apiClient.get<MatchResponse[]>('/matches');
        setMatches(res.data);
      } catch (err) {
        console.error('Failed to fetch matches', err);
      } finally {
        setIsLoading(false);
      }
    };
    fetchMatches();
  }, []);

  const liveMatches = matches.filter((m) => m.status === 'Live');
  const upcomingMatches = matches.filter((m) => m.status === 'Scheduled');
  const pastMatches = matches.filter((m) => m.status === 'Ended');

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {isLoading ? (
        <div className="flex justify-center py-20">
          <div className="w-8 h-8 border-4 border-[#00B8FC] border-t-transparent rounded-full animate-spin"></div>
        </div>
      ) : (
        <>
          {liveMatches.length > 0 && <MatchList title="Live Now" matches={liveMatches} />}
          <MatchList title="Upcoming Matches" matches={upcomingMatches} />
          {pastMatches.length > 0 && <MatchList title="Completed" matches={pastMatches} />}
        </>
      )}
    </div>
  );
};

export default MatchesView;
