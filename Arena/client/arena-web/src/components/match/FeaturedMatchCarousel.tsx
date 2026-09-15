import React, { useState } from 'react';
import { ChevronLeft, ChevronRight, Volume2, Maximize2, Settings, Users } from 'lucide-react';

const carouselData = [
  {
    id: 1,
    video: '/video1.mp4',
    broadcaster: 'HollowVOD',
    initials: 'HV',
    subtitle: 'Grand Finals • Day 2',
    viewers: '12,480',
    tags: ['League of Legends', 'Live'],
    description: 'Grand Finals Game 5: Silver Scrapes decider on Summoner Rift.',
  },
  {
    id: 2,
    video: '/video3.mp4',
    broadcaster: 'CrimsonVX',
    initials: 'CV',
    subtitle: 'Pro Series • Week 4',
    viewers: '8,204',
    tags: ['FPS', 'Tactical'],
    description: 'Legendary Solo Campaign: Full LASO run with all skulls active.',
  },
  {
    id: 3,
    video: '/video4.mp4',
    broadcaster: 'AeroRush',
    initials: 'AR',
    subtitle: 'Speedrun Showcase',
    viewers: '15,932',
    tags: ['Speedrun', 'Any'],
    description: 'Conquest 128 Havoc: Flanking urban choke points and heavy armor mastery!',
  },
  {
    id: 4,
    video: '/video5.mp4',
    broadcaster: 'FluxMain',
    initials: 'FM',
    subtitle: 'Campaign',
    viewers: '4,129',
    tags: ['Story', 'Horror'],
    description: 'Hardcore Blind Run: Zero saves, scarce ammo, and maximum terror.',
  }
];

export const FeaturedMatchCarousel: React.FC = () => {
  const [currentIndex, setCurrentIndex] = useState(0);

  const handlePrev = () => {
    setCurrentIndex((prev) => (prev === 0 ? carouselData.length - 1 : prev - 1));
  };

  const handleNext = () => {
    setCurrentIndex((prev) => (prev === carouselData.length - 1 ? 0 : prev + 1));
  };

  const getSlideIndex = (offset: number) => {
    const len = carouselData.length;
    return (currentIndex + offset + len) % len;
  };

  const activeSlide = carouselData[currentIndex];
  const leftSlide = carouselData[getSlideIndex(-1)];
  const rightSlide = carouselData[getSlideIndex(1)];

  return (
    <section className="w-full py-6 px-4 border-b border-[var(--line)] bg-[var(--bg)] transition-colors select-none">
      <div className="relative max-w-7xl mx-auto flex items-center justify-center gap-6">

        {/* Left Flanking Inactive Slide */}
        <div
          className="hidden xl:flex w-48 h-[340px] flex-shrink-0 bg-[var(--panel)] border border-[var(--line)] rounded-md opacity-30 scale-90 items-center justify-center overflow-hidden cursor-pointer transition-all hover:opacity-50"
          onClick={handlePrev}
        >
          <video src={leftSlide.video} autoPlay loop muted playsInline className="w-full h-full object-cover mix-blend-screen" />
        </div>

        {/* Active Center Card (70/30 Unified Split) */}
        <div className="flex flex-col lg:flex-row w-full max-w-4xl h-auto lg:h-[420px] bg-[var(--panel)] border border-[var(--line)] rounded-md overflow-hidden shadow-2xl z-10 transition-colors relative">

          {/* Left: Video Pane (70%) */}
          <div className="relative flex-[7] bg-black aspect-video lg:aspect-auto flex items-center justify-center overflow-hidden border-b lg:border-b-0 lg:border-r border-[var(--line)]">
            <div className="absolute top-3 left-3 z-20 flex items-center gap-2">
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-black/80 border border-[#FF2B56]/50 rounded text-white text-[11px] font-bold font-mono tracking-wider backdrop-blur-md">
                <span className="w-2 h-2 rounded-full bg-[#FF2B56] shadow-[0_0_8px_#FF2B56] animate-pulse"></span>
                LIVE NOW
              </span>
            </div>

            {/* Video Placeholder / Stream Feed */}
            <div className="w-full h-full flex items-center justify-center bg-[#05070A] overflow-hidden">
              <video
                key={activeSlide.id}
                autoPlay
                loop
                muted
                playsInline
                className="w-full h-full object-cover opacity-80 mix-blend-screen"
                src={activeSlide.video}
              />
            </div>

            {/* Player Controls Bar */}
            <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-black/90 via-black/40 to-transparent p-3 flex items-center justify-between text-white text-xs z-20">
              <div className="flex items-center gap-3">
                <Volume2 className="w-4 h-4 cursor-pointer hover:text-[var(--prime)]" />
                <span className="font-mono text-[10px] bg-white/10 px-1.5 py-0.5 rounded">1080P 60FPS</span>
              </div>
              <div className="flex items-center gap-3">
                <Settings className="w-4 h-4 cursor-pointer hover:text-[var(--prime)]" />
                <Maximize2 className="w-4 h-4 cursor-pointer hover:text-[var(--prime)]" />
              </div>
            </div>
          </div>

          {/* Right: Broadcaster & Match Metadata Drawer (30%) */}
          <div className="flex-[3] p-5 flex flex-col justify-between bg-[var(--panel)] transition-colors">
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <div className="w-11 h-11 rounded-full bg-[var(--panel-2)] border-2 border-[var(--prime)] flex items-center justify-center font-mono font-bold text-xs text-[var(--text)]">
                  {activeSlide.initials}
                </div>
                <div>
                  <h4 className="text-sm font-bold text-[var(--text)] tracking-wide">{activeSlide.broadcaster}</h4>
                  <p className="text-xs text-[var(--subtext)]">{activeSlide.subtitle}</p>
                </div>
              </div>

              <div className="flex items-center gap-2 text-xs text-[var(--subtext)] font-mono">
                <Users className="w-3.5 h-3.5 text-[var(--prime)]" />
                <span className="text-[var(--text)] font-semibold">{activeSlide.viewers}</span> viewers
              </div>

              <div className="flex flex-wrap gap-1.5">
                {activeSlide.tags.map(tag => (
                  <span key={tag} className="text-[10px] uppercase font-mono px-2 py-0.5 bg-[var(--panel-2)] border border-[var(--line)] text-[var(--subtext)] rounded">
                    {tag}
                  </span>
                ))}
              </div>

              <p className="text-xs text-[var(--subtext)] leading-relaxed line-clamp-3">
                {activeSlide.description}
              </p>
            </div>

            <button className="w-full py-2.5 mt-4 bg-[var(--prime)] hover:bg-[var(--prime-dim)] text-black font-bold uppercase tracking-wider text-xs rounded transition-colors shadow-[0_0_15px_-3px_rgba(0,184,252,0.4)]">
              Watch in Arena
            </button>
          </div>
        </div>

        {/* Right Flanking Inactive Slide */}
        <div
          className="hidden xl:flex w-48 h-[340px] flex-shrink-0 bg-[var(--panel)] border border-[var(--line)] rounded-md opacity-30 scale-90 items-center justify-center overflow-hidden cursor-pointer transition-all hover:opacity-50"
          onClick={handleNext}
        >
          <video src={rightSlide.video} autoPlay loop muted playsInline className="w-full h-full object-cover mix-blend-screen" />
        </div>

        {/* Navigation Chevrons */}
        <button
          onClick={handlePrev}
          className="absolute left-1 top-1/2 -translate-y-1/2 p-2 rounded-full bg-black/70 border border-[var(--line)] text-white hover:bg-[var(--prime)] hover:text-black transition-colors z-20"
        >
          <ChevronLeft className="w-5 h-5" />
        </button>
        <button
          onClick={handleNext}
          className="absolute right-1 top-1/2 -translate-y-1/2 p-2 rounded-full bg-black/70 border border-[var(--line)] text-white hover:bg-[var(--prime)] hover:text-black transition-colors z-20"
        >
          <ChevronRight className="w-5 h-5" />
        </button>
      </div>
    </section>
  );
};

export default FeaturedMatchCarousel;