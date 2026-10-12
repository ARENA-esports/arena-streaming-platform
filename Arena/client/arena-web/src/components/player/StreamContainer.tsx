import React, { useState, useEffect } from 'react';
import { TwitchPlayer } from '../player/TwitchPlayer';

interface StreamContainerProps {
    apiChannelName?: string;
    isPlaying?: boolean;
    isHeartbeatEligible?: boolean;
    userRole?: string;
    hasValidStream?: boolean;
    isAuthenticated?: boolean;
    onPlay?: () => void;
    onPause?: () => void;
    onChannelChange?: (channelName: string) => void;
}

export const StreamContainer: React.FC<StreamContainerProps> = ({
    apiChannelName,
    isPlaying = false,
    isHeartbeatEligible,
    userRole,
    hasValidStream = true,
    isAuthenticated = true,
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
        onPause?.();
    }, [apiChannelName, isMockOrEmpty, envOverrideChannel, onChannelChange, onPause]);

    const handleSelectChannel = (channel: string) => {
        const trimmed = channel.trim();
        if (!trimmed) return;
        onPause?.();
        setDevChannel(trimmed);
        setInputVal(trimmed);
        onChannelChange?.(trimmed);
    };

    const handleTwitchPlay = () => {
        onPlay?.();
    };

    const handleTwitchPause = () => {
        onPause?.();
    };

    const activeChannel = devChannel;

    return (
        <div className="flex flex-col w-full h-full gap-2">
            {/* Twitch Stream Channel & Playback Control Bar */}
            <div className='flex flex-wrap items-center justify-between gap-2 p-2.5 bg-arena-card/90 rounded-md text-xs shadow-md'>
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

                {/* Live Watching Status & Playback Controls */}
                <div className="flex items-center gap-2">
                    {isPlaying ? (
                        (isHeartbeatEligible === true || isHeartbeatEligible === undefined) ? (
                            <div className="flex items-center gap-1.5 px-2.5 py-1 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 rounded text-[11px] font-mono font-semibold">
                                <span className="w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
                                <span>LIVE WATCHING (+10 coins/min)</span>
                            </div>
                        ) : !isAuthenticated ? (
                            <div className="flex items-center gap-1.5 px-2.5 py-1 bg-sky-500/10 border border-sky-500/30 text-sky-400 rounded text-[11px] font-mono font-semibold">
                                <span className="w-2 h-2 rounded-full bg-sky-400" />
                                <span>STREAM PLAYING (Sign In to Earn)</span>
                            </div>
                        ) : userRole && userRole.toLowerCase() !== 'viewer' ? (
                            <div className="flex items-center gap-1.5 px-2.5 py-1 bg-amber-500/10 border border-amber-500/30 text-amber-400 rounded text-[11px] font-mono font-semibold" title="Only Viewer accounts are eligible for watch-time coin rewards">
                                <span className="w-2 h-2 rounded-full bg-amber-400" />
                                <span>STREAM PLAYING ({userRole.toUpperCase()})</span>
                            </div>
                        ) : !hasValidStream ? (
                            <div className="flex items-center gap-1.5 px-2.5 py-1 bg-amber-500/10 border border-amber-500/30 text-amber-400 rounded text-[11px] font-mono font-semibold">
                                <span className="w-2 h-2 rounded-full bg-amber-400" />
                                <span>STREAM PLAYING (No Match Stream)</span>
                            </div>
                        ) : (
                            <div className="flex items-center gap-1.5 px-2.5 py-1 bg-arena-bg border border-arena-border text-gray-300 rounded text-[11px] font-mono">
                                <span className="w-2 h-2 rounded-full bg-gray-400" />
                                <span>STREAM PLAYING</span>
                            </div>
                        )
                    ) : (
                        <div className="flex items-center gap-1.5 px-2.5 py-1 bg-arena-bg border border-arena-border text-gray-400 rounded text-[11px] font-mono">
                            <span className="w-2 h-2 rounded-full bg-gray-500" />
                            <span>STREAM PAUSED / OFFLINE</span>
                        </div>
                    )}

                    {isPlaying ? (
                        <button
                            type="button"
                            onClick={handleTwitchPause}
                            className="px-3 py-1 bg-amber-500/20 border border-amber-500/40 text-amber-300 hover:bg-amber-500/30 font-semibold rounded text-xs transition-colors flex items-center gap-1"
                        >
                            ⏸ Pause
                        </button>
                    ) : (
                        <button
                            type="button"
                            onClick={handleTwitchPlay}
                            className="px-3 py-1 bg-emerald-500 text-black hover:bg-emerald-400 font-bold rounded text-xs transition-colors flex items-center gap-1 shadow-[0_0_12px_rgba(16,185,129,0.3)]"
                        >
                            ▶ Watch Stream
                        </button>
                    )}
                </div>
            </div>

            {/* Embedded Player Tile */}
            <TwitchPlayer
                channel={activeChannel}
                onPlay={handleTwitchPlay}
                onPause={handleTwitchPause}
            />
            <div className="flex justify-between items-center text-[10px] text-gray-400 px-1 mt-0.5">
                <span>
                    {isPlaying ? (
                        (isHeartbeatEligible === true || isHeartbeatEligible === undefined) ? (
                            <span className="text-emerald-400 font-medium">● Watch active — Earning +10 coins every minute</span>
                        ) : !isAuthenticated ? (
                            <span className="text-sky-400">Sign in with a Viewer account to earn watch-time coins</span>
                        ) : userRole && userRole.toLowerCase() !== 'viewer' ? (
                            <span className="text-amber-400">Viewing as {userRole} — Only Viewer accounts earn coin rewards</span>
                        ) : !hasValidStream ? (
                            <span className="text-amber-400">Match has no official broadcast linked — rewards inactive</span>
                        ) : (
                            <span className="text-gray-400">Playback active</span>
                        )
                    ) : (
                        'Click "Watch Stream" or load live broadcast to begin earning coins'
                    )}
                </span>
                <span>Seeing Error? Disable ad-blocker or tracking prevention for Twitch embed</span>
            </div>
        </div>
    );
};