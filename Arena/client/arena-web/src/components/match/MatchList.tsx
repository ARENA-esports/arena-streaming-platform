import React from 'react';
import { MatchResponse } from '../../types';
import MatchCard from './MatchCard';

interface MatchListProps {
  matches: MatchResponse[];
  title: string;
}

export const MatchList: React.FC<MatchListProps> = ({ matches, title }) => {
  return (
    <div className="mb-12">
      <div className="flex items-center mb-6">
        <h2 className="text-xl font-semibold text-white">{title}</h2>
        <div className="ml-4 h-[1px] bg-arena-border flex-grow"></div>
      </div>
      
      {matches.length === 0 ? (
        <div className="bg-arena-surface border border-arena-border border-dashed rounded-sm p-12 text-center">
          <p className="text-arena-textMuted font-sans">No matches found for this category.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {matches.map((match) => (
            <MatchCard key={match.matchId} match={match} />
          ))}
        </div>
      )}
    </div>
  );
};

export default MatchList;
