import React, { useState } from 'react';
import { TwitchPlayer } from '../player/TwitchPlayer';

interface StreamContainerProps {
    apiChannelName?: string;
}

export const StreamContainer: React.FC<StreamContainerProps> = ({ apiChannelName }) => {
    // Fall back to VITE_TWITCH_TEST_CHANNEL or default channel in development
    const initialDevChannel = import.meta.env.VITE_TWITCH_TEST_CHANNEL || 'shroud';
    const [devChannel, setDevChannel] = useState<string>(initialDevChannel);
    const [inputVal, setInputVal] = useState<string>(initialDevChannel);

    // Use manual dev channel if in dev mode; otherwise use real backend channel
    const activeChannel = import.meta.env.DEV ? devChannel : apiChannelName || '';

    return (
        <div className="flex flex-col w-full h-full gap-2">
            {/* Dev-only manual test bar (stripped automatically in production builds) */}
            {import.meta.env.DEV && (
                <div className='flex items-center gap-2 p-2 bg-arena-card border border-arena-border rounded text-xs'>
                    <span className="text-arena-cyan font-mono font-bold">DEV OVERRIDE</span>
                    <input
                        type="text"
                        value={inputVal}
                        onChange={(e) => setInputVal(e.target.value)}
                        placeholder="Type live Twitch channel..."
                        className="bg-arena-bg px-2 py-1 rounded border border-arena-border text-white focus:outline-none focus:border-arena-cyan"
                    />
                    <button
                        type="button"
                        onClick={() => setDevChannel(inputVal)}
                        className="px-2 py-1 bg-arena-cyan/20 text-arena-cyan hover:bg-arena-cyan/30 rounded transition-colors">
                        Load Stream
                    </button>
                    <span className="text-gray-400">Current: <strong>{activeChannel}</strong></span>
                </div>
            )}

            {/* Embedded Player Tile */}
            <TwitchPlayer channel={activeChannel} />
            <p className="text-[10px] text-white text-center mt-1">
                Seeing Error? Please disable your ad-blocker or tracking prevention for this site.
            </p>
        </div>
    );
};