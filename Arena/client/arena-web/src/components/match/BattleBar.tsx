import React from 'react';

interface BattleBarProps {
    matchId: number | string;
}

export const BattleBar: React.FC<BattleBarProps> = ({ matchId }) => (
    <div className="w-full h-12 bg-arena-card border border-arena-border rounded flex items-center justify-center text-sm text-gray-400">
        BattleBar Placeholder (Match {matchId})
    </div>
);

export default BattleBar;
