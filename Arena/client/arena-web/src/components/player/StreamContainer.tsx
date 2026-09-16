import React, { useState, useEffect } from 'react';
import { TwitchPlayer } from '../player/TwitchPlayer';

interface StreamContainerProps {
    apiChannelName?: string;
}

export const StreamContainer: React.FC<StreamContainerProps> = ({ apiChannelName }) => {
    // Check if channel from API is missing or a mock channel
    const isMockOrEmpty = !apiChannelName || apiChannelName.startsWith('mock_');
    const envOverrideChannel = import.meta.env.VITE_TWITCH_TEST_CHANNEL;
    
    // Default initial channel prioritization:
    // 1. env override channel if provided
    // 2. api channel if valid (not mock)
    // 3. Fallback dev channel ('Arena_streams')
    const initialChannel = envOverrideChannel || (!isMockOrEmpty ? apiChannelName : '') || 'Arena_streams';

    const [devChannel, setDevChannel] = useState<string>(initialChannel);
    const [inputVal, setInputVal] = useState<string>(initialChannel);

    useEffect(() => {
        if (!isMockOrEmpty && apiChannelName && !envOverrideChannel) {
            setDevChannel(apiChannelName);
            setInputVal(apiChannelName);
        }
    }, [apiChannelName, isMockOrEmpty, envOverrideChannel]);

    const activeChannel = devChannel;

    return (
        <div className="flex flex-col w-full h-full gap-2">
            {/* Twitch Stream Channel Override Bar */}
            <div className='flex flex-wrap items-center gap-2 p-2.5 bg-arena-card/90 border border-arena-cyan/30 rounded-md text-xs shadow-md'>
                <span className="bg-arena-cyan/20 text-arena-cyan px-2 py-0.5 rounded font-mono font-bold uppercase tracking-wider text-[10px]">
                    CHANNEL
                </span>
                <div className="flex items-center gap-1.5 flex-1 min-w-[200px]">
                    <input
                        type="text"
                        value={inputVal}
                        onChange={(e) => setInputVal(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') setDevChannel(inputVal);
                        }}
                        placeholder="Type live Twitch channel..."
                        className="w-full bg-arena-bg px-2.5 py-1 rounded border border-arena-border text-white placeholder-gray-500 focus:outline-none focus:border-arena-cyan text-xs font-mono"
                    />
                    <button
                        type="button"
                        onClick={() => setDevChannel(inputVal)}
                        className="px-3 py-1 bg-arena-cyan text-black font-semibold hover:bg-arena-cyan/80 rounded transition-colors whitespace-nowrap">
                        Load Stream
                    </button>
                </div>

                {/* Quick Presets */}
                <div className="flex items-center gap-1">
                    <span className="text-gray-400 text-[11px] hidden sm:inline">Presets:</span>
                    {['Arena_streams', 'riotgames', 'esl_csgo', 'shroud'].map((preset) => (
                        <button
                            key={preset}
                            type="button"
                            onClick={() => {
                                setInputVal(preset);
                                setDevChannel(preset);
                            }}
                            className={`px-2 py-0.5 rounded text-[11px] font-mono transition-colors ${
                                activeChannel === preset
                                    ? 'bg-arena-cyan/30 text-arena-cyan border border-arena-cyan/50 font-bold'
                                    : 'bg-arena-bg text-gray-400 hover:text-white border border-arena-border'
                            }`}
                        >
                            {preset}
                        </button>
                    ))}
                </div>
            </div>

            {/* Embedded Player Tile */}
            <TwitchPlayer channel={activeChannel} />
            <p className="text-[10px] text-gray-400 text-center mt-1">
                Seeing Error? Please disable your ad-blocker or tracking prevention for this site.
            </p>
        </div>
    );
};