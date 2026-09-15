import React, { useMemo } from 'react';

interface TwitchEmbedProps {
  url: string;
}

export const TwitchEmbed: React.FC<TwitchEmbedProps> = ({ url }) => {
  //hard: CWE-1021 & CWE-601: Strictly validate embed source protocol and domain to prevent rendering malicious or phishing iframes
  const sanitizedUrl = useMemo(() => {
    if (!url || typeof url !== 'string') return null;

    try {
      const parsed = new URL(url);

      //hard: Enforce HTTPS scheme and official Twitch player hostname
      if (parsed.protocol !== 'https:' || parsed.hostname !== 'player.twitch.tv') {
        return null;
      }

      //hard: ToS & Local Dev Compatibility: Ensure the parent parameter matches the current browser hostname (e.g., 'localhost' during dev)
      const currentHost = typeof window !== 'undefined' ? window.location.hostname : 'localhost';
      const parents = parsed.searchParams.getAll('parent');
      if (!parents.includes(currentHost)) {
        parsed.searchParams.append('parent', currentHost);
      }

      return parsed.toString();
    } catch {
      return null;
    }
  }, [url]);

  //hard: Fail-safe fallback UI prevents blank pages or application crashes on invalid or malicious embed URLs
  if (!sanitizedUrl) {
    return (
      <div className="w-full aspect-video bg-arena-bg border border-red-500/40 rounded-sm flex flex-col items-center justify-center text-gray-400 p-4 text-center">
        <p className="text-red-400 text-sm font-semibold mb-1">Untrusted or Invalid Stream Source</p>
        <p className="text-xs text-gray-500">Only official Twitch player embeds are permitted.</p>
      </div>
    );
  }

  return (
    <div className="w-full aspect-video bg-arena-bg border border-arena-border relative rounded-sm overflow-hidden shadow-[0_0_30px_rgba(0,184,252,0.1)] group">
      <iframe
        src={sanitizedUrl}
        allowFullScreen
        //hard: Principle of Least Privilege: Exclude allow-top-navigation to prevent third-party ads from hijacking the parent window
        //hard: Included allow-presentation and allow-modals so Twitch player controls, chat logins, and fullscreen work during local testing
        sandbox="allow-scripts allow-same-origin allow-popups allow-presentation allow-modals"
        //hard: Restrict referrer leakage to third-party CDNs and ad providers
        referrerPolicy="strict-origin-when-cross-origin"
        width="100%"
        height="100%"
        className="absolute inset-0"
        title="Twitch Stream"
      ></iframe>

      {/* Subtle overlay border that glows on hover */}
      <div className="absolute inset-0 border-2 border-transparent group-hover:border-arena-cyan/20 pointer-events-none transition-colors duration-300"></div>
    </div>
  );
};

export default TwitchEmbed;