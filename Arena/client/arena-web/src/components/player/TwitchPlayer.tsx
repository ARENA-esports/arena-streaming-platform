import React, { useMemo } from "react";
// ask for promps to use twitch player components
export interface TwitchPlayerProps {
    channel: string;
    parentDomains?: string[];
    className?: string;
}
// declare componens and only accept props matching the interface
export const TwitchPlayer: React.FC<TwitchPlayerProps> = ({
    channel,
    parentDomains,
    className = '',
}) => {

    const resolvedDomains = useMemo(() => {
        // exact comma separate domains from vite environment config
        const envDomains = (import.meta.env.VITE_APP_DOMAIN || '')
            .split(',')
            .map((d: string) => d.trim())
            .filter(Boolean);
        const currentHost = typeof window !== 'undefined' ? window.location.hostname : 'localhost';
        return Array.from(new Set([...(parentDomains || []), ...envDomains, currentHost]));
    }, [parentDomains]);
    // once resolvedDomains is ready we build the embed URL for the twitch iframe
    const embedUrl = useMemo(() => {
        const trimmedChannel = channel?.trim();
        if (!trimmedChannel) return '';

        const parentParams = resolvedDomains
            .map((domain) => `parent=${encodeURIComponent(domain)}`)
            .join('&')

        return `https://player.twitch.tv/?channel=${encodeURIComponent(trimmedChannel)}&${parentParams}&autoplay=true&muted=false`;
        // render iframe using recommended attributes
    }, [channel, resolvedDomains]);

    // Implement a Fail-Safe Fallback State
    // if no channel is provided, render a placeholder 
    if (!embedUrl) {
        return (
            <div className={`w-full aspect-video bg-arena-bg border border-arena-border rounded-sm flex flex-col items-center justify-center text-gray-400 ${className}`}>
                <p className="text-sm font-medium">No active broadcast selected</p>
                <p className="text-xs text-gray-500 mt-1">Select a tournament match to view stream</p>
            </div>
        );
    }
    // render the Isolated 16:9 Container and iframe and pass props
    return (
        <div className={`relative w-full aspect-video bg-black rounded-sm overflow-hidden border border-arena-border ${className}`}>
            <iframe
                src={embedUrl}
                title={`Twitch Stream - ${channel}`}
                allowFullScreen
                sandbox="allow-scripts allow-same-origin allow-presentation allow-popups allow-modals"
                referrerPolicy="strict-origin-when-cross-origin"
                className="absolute inset-0 w-full h-full border-0">

            </iframe>
        </div>
    );
};
// Export the Component for Consumption
export default TwitchPlayer;