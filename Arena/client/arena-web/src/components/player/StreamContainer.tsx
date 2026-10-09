import React, { useState, useEffect } from 'react';
import { TwitchPlayer } from '../player/TwitchPlayer';

interface StreamContainerProps {
    apiChannelName?: string;
    isPlaying?: boolean;
    onPlay?: () => void;
    onPause?: () => void;
    onChannelChange?: (channelName: string) => void;
}

export const StreamContainer: React.FC<StreamContainerProps> = ({
    apiChannelName,
    onPlay,
    onPause,
    onChannelChange,
}) => {
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
        const nextChannel = (!isMockOrEmpty && apiChannelName && !envOverrideChannel)
            ? apiChannelName
            : initialChannel;
        setDevChannel(nextChannel);
        setInputVal(nextChannel);
        onChannelChange?.(nextChannel);
        onPlay?.();
    }, [apiChannelName, isMockOrEmpty, envOverrideChannel, onPlay]);

    const handleSelectChannel = (channel: string) => {
        const trimmed = channel.trim();
        if (!trimmed) return;
        setDevChannel(trimmed);
        setInputVal(trimmed);
        onChannelChange?.(trimmed);
        onPlay?.();
    };


    const handleTwitchPlay = () => {
        onPlay?.();
    };

    const handleTwitchPause = () => {
        // If the channel is a mock or empty dev fallback channel, the Twitch embed is offline.
        // We do not let a mock channel's offline embed pause kill watch reward heartbeats for viewers in a live match.
        if (!isMockOrEmpty) {
            onPause?.();
        }
    };

    const activeChannel = devChannel;

    return (
        <div className="flex flex-col w-full h-full gap-2">
            {/* Twitch Stream Channel & Playback Control Bar */}
            <div className='flex flex-wrap items-center justify-between gap-2 p-2.5 bg-arena-card/90 rounded-md text-xs shadow-md border border-arena-border/50'>
                <div className="flex items-center gap-2 flex-wrap">
                    <span className="bg-arena-cyan/20 text-arena-cyan px-2 py-0.5 rounded font-mono font-bold uppercase tracking-wider text-[10px]">
                        CHANNEL
                    </span>
                    <div className="flex items-center gap-1.5 min-w-[200px]">
                        <input
                            type="text"
                            value={inputVal}
                            onChange={(e) => setInputVal(e.target.value)}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') handleSelectChannel(inputVal);
                            }}
                            placeholder="Type live Twitch channel..."
                            className="bg-arena-bg px-2.5 py-1 rounded border border-arena-border text-white placeholder-gray-500 focus:outline-none focus:border-arena-cyan text-xs font-mono w-44 sm:w-56"
                        />
                        <button
                            type="button"
                            onClick={() => handleSelectChannel(inputVal)}
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
                                onClick={() => handleSelectChannel(preset)}
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
            </div>

            {/* Embedded Player Tile */}
            <TwitchPlayer
                channel={activeChannel}
                onPlay={handleTwitchPlay}
                onPause={handleTwitchPause}
            />
            <div className="flex justify-between items-center text-[10px] text-gray-400 px-1 mt-0.5">
                <span>Earn +10 coins every minute while watching live matches</span>
                <span>Seeing Error? Disable ad-blocker or tracking prevention for Twitch embed</span>
            </div>
        </div>
    );
};