import React from 'react';
import { ChevronLeft, ChevronRight, Volume2, Maximize2, Settings, Users } from 'lucide-react';

export const FeaturedMatchCarousel: React.FC = () => {
  return (
    <section className="w-full py-6 px-4 border-b border-[var(--line)] bg-[var(--bg)] transition-colors select-none">
      <div className="relative max-w-7xl mx-auto flex items-center justify-center gap-6">
        
        {/* Left Flanking Inactive Slide */}
        <div className="hidden xl:flex w-48 h-[340px] flex-shrink-0 bg-[var(--panel)] border border-[var(--line)] rounded-md opacity-30 scale-90 items-center justify-center pointer-events-none">
          <span className="text-xs font-mono text-[var(--muted)] uppercase tracking-widest">Sample Stream</span>
        </div>

        {/* Active Center Card (70/30 Unified Split) */}
        <div className="flex flex-col lg:flex-row w-full max-w-4xl h-auto lg:h-[420px] bg-[var(--panel)] border border-[var(--line)] rounded-md overflow-hidden shadow-2xl z-10 transition-colors">
          
          {/* Left: Video Pane (70%) */}
          <div className="relative flex-[7] bg-black aspect-video lg:aspect-auto flex items-center justify-center overflow-hidden border-b lg:border-b-0 lg:border-r border-[var(--line)]">
            <div className="absolute top-3 left-3 z-20 flex items-center gap-2">
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-black/80 border border-[#FF2B56]/50 rounded text-white text-[11px] font-bold font-mono tracking-wider backdrop-blur-md">
                <span className="w-2 h-2 rounded-full bg-[#FF2B56] shadow-[0_0_8px_#FF2B56] animate-pulse"></span>
                LIVE NOW
              </span>
            </div>

            {/* Video Placeholder / Stream Feed */}
            <div className="w-full h-full flex items-center justify-center bg-[#05070A]">
              <span className="text-xs font-mono text-zinc-500 uppercase tracking-widest border border-dashed border-zinc-800 px-4 py-2 rounded">
                Live Broadcast Feed
              </span>
            </div>

            {/* Player Controls Bar */}
            <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-black/90 via-black/40 to-transparent p-3 flex items-center justify-between text-white text-xs">
              <div className="flex items-center gap-3">
                <Volume2 className="w-4 h-4 cursor-pointer hover:text-[var(--prime)]"/>
                <span className="font-mono text-[10px] bg-white/10 px-1.5 py-0.5 rounded">1080P 60FPS</span>
              </div>
              <div className="flex items-center gap-3">
                <Settings className="w-4 h-4 cursor-pointer hover:text-[var(--prime)]"/>
                <Maximize2 className="w-4 h-4 cursor-pointer hover:text-[var(--prime)]"/>
              </div>
            </div>
          </div>

          {/* Right: Broadcaster & Match Metadata Drawer (30%) */}
          <div className="flex-[3] p-5 flex flex-col justify-between bg-[var(--panel)] transition-colors">
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <div className="w-11 h-11 rounded-full bg-[var(--panel-2)] border-2 border-[var(--prime)] flex items-center justify-center font-mono font-bold text-xs text-[var(--text)]">
                  HV
                </div>
                <div>
                  <h4 className="text-sm font-bold text-[var(--text)] tracking-wide">HollowVOD</h4>
                  <p className="text-xs text-[var(--subtext)]">Grand Finals • Day 2</p>
                </div>
              </div>

              <div className="flex items-center gap-2 text-xs text-[var(--subtext)] font-mono">
                <Users className="w-3.5 h-3.5 text-[var(--prime)]"/>
                <span className="text-[var(--text)] font-semibold">12,480</span> viewers
              </div>

              <div className="flex flex-wrap gap-1.5">
                <span className="text-[10px] uppercase font-mono px-2 py-0.5 bg-[var(--panel-2)] border border-[var(--line)] text-[var(--subtext)] rounded">Tactical</span>
                <span className="text-[10px] uppercase font-mono px-2 py-0.5 bg-[var(--panel-2)] border border-[var(--line)] text-[var(--subtext)] rounded">BO5</span>
              </div>

              <p className="text-xs text-[var(--subtext)] leading-relaxed line-clamp-3">
                Hollow Sigil vs Ninefold. Elimination bracket decider with regional seeding on the line.
              </p>
            </div>

            <button className="w-full py-2.5 mt-4 bg-[var(--prime)] hover:bg-[var(--prime-dim)] text-black font-bold uppercase tracking-wider text-xs rounded transition-colors shadow-[0_0_15px_-3px_rgba(0,184,252,0.4)]">
              Watch in Arena
            </button>
          </div>
        </div>

        {/* Right Flanking Inactive Slide */}
        <div className="hidden xl:flex w-48 h-[340px] flex-shrink-0 bg-[var(--panel)] border border-[var(--line)] rounded-md opacity-30 scale-90 items-center justify-center pointer-events-none">
          <span className="text-xs font-mono text-[var(--muted)] uppercase tracking-widest">Sample Stream</span>
        </div>

        {/* Navigation Chevrons */}
        <button className="absolute left-1 top-1/2 -translate-y-1/2 p-2 rounded-full bg-black/70 border border-[var(--line)] text-white hover:bg-[var(--prime)] hover:text-black transition-colors z-20">
          <ChevronLeft className="w-5 h-5"/>
        </button>
        <button className="absolute right-1 top-1/2 -translate-y-1/2 p-2 rounded-full bg-black/70 border border-[var(--line)] text-white hover:bg-[var(--prime)] hover:text-black transition-colors z-20">
          <ChevronRight className="w-5 h-5"/>
        </button>
      </div>
    </section>
  );
};

export default FeaturedMatchCarousel;