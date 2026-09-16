import { FC, useEffect, useState } from 'react';
import { MatchScheduleResponse } from '../types';
import { apiClient } from '../api/client';
import MatchList from '../components/match/MatchList';
import FeaturedMatchCarousel from '../components/match/FeaturedMatchCarousel';

export const HomeView: FC = () => {
  const [matches, setMatches] = useState<MatchScheduleResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchMatches = async () => {
      try {
        const [liveRes, scheduledRes, pastRes] = await Promise.all([
          apiClient.get<MatchScheduleResponse[]>('/matches', { params: { status: 'Live' } }),
          apiClient.get<MatchScheduleResponse[]>('/matches', { params: { status: 'Scheduled' } }),
          apiClient.get<MatchScheduleResponse[]>('/matches', { params: { status: 'Ended' } })
        ]);
        
        setMatches([...liveRes.data, ...scheduledRes.data, ...pastRes.data]);
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
    <div>
      <section className="featured-match-section">
        <FeaturedMatchCarousel />
      </section>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {isLoading ? (
          <div className="flex justify-center py-20">
            <div className="w-8 h-8 border-4 border-[#00B8FC] border-t-transparent rounded-full animate-spin"></div>
          </div>
        ) : (
          <>
            <MatchList title="Live Now" matches={liveMatches} />
            <MatchList title="Upcoming Matches" matches={upcomingMatches} />
            {pastMatches.length > 0 && <MatchList title="Completed Matches" matches={pastMatches} />}
          </>
        )}
      </div>
    </div>
  );
};

export default HomeView;
