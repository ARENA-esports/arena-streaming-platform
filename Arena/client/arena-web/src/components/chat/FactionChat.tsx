import React from 'react';

interface FactionChatProps {
    matchId: number | string;
}

export const FactionChat: React.FC<FactionChatProps> = ({ matchId }) => (
    <div className="w-full h-full min-h-[400px] bg-arena-card border border-arena-border rounded flex flex-col items-center justify-center text-sm text-gray-400 p-4 text-center">
        FactionChat Placeholder<br/>(Match {matchId})
    </div>
);

export default FactionChat;
