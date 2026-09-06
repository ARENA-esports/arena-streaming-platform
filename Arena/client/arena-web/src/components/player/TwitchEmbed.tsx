import React from 'react';

interface TwitchEmbedProps {
  url: string;
}

export const TwitchEmbed: React.FC<TwitchEmbedProps> = ({ url }) => {
  return (
    <div className="w-full aspect-video bg-arena-bg border border-arena-border relative rounded-sm overflow-hidden shadow-[0_0_30px_rgba(0,184,252,0.1)] group">
      <iframe
        src={url}
        allowFullScreen
        sandbox="allow-scripts allow-same-origin allow-popups"
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
