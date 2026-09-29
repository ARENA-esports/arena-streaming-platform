import React from 'react';

interface TeamSelectorProps {
    matchId: number | string;
}

export const TeamSelector: React.FC<TeamSelectorProps> = ({ matchId }) => (
    <div className="w-full h-24 bg-arena-card border border-arena-border rounded flex items-center justify-center text-sm text-gray-400">
        TeamSelector Placeholder (Match {matchId})
    </div>
);

export default TeamSelector;
