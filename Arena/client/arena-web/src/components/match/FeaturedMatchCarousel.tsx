import { FC } from 'react';
import { ChevronLeft, ChevronRight, Volume2, Maximize2, Settings, Users } from 'lucide-react';

export const FeaturedMatchCarousel: FC = () => {
  return (
    <div className="w-full max-w-7xl mx-auto px-4 py-8 overflow-hidden relative">
      <div className="flex flex-row items-center justify-center relative">
        
        {/* Left Flanking Card (Preview) */}
        <div className="w-56 h-[340px] flex-shrink-0 hidden lg:flex items-center justify-center bg-[#0D1117] border border-[#2A2F38] rounded-md opacity-30 scale-90">
          <span className="text-xs font-mono text-zinc-600 uppercase tracking-widest">sample video</span>
        </div>

        {/* Center Active Card (Dual-Pane Split) */}
        <div className="w-full max-w-4xl h-[420px] bg-[#0D1117] border border-[#2A2F38] rounded-md overflow-hidden flex flex-col md:flex-row shadow-2xl z-10 mx-4">
          
          {/* Left Pane (~70% Width) */}
          <div className="relative flex-1 bg-black flex items-center justify-center border-b md:border-b-0 md:border-r border-[#2A2F38]">
            {/* Top Left Overlay: Spotlight Tag */}
            <div className="absolute top-4 left-4 z-20">
              <div className="spotlight-tag">
                <span className="pulse-dot"></span>LIVE NOW
              </div>
            </div>

            {/* Center Content Placeholder */}
            <span className="text-sm font-mono text-zinc-500 uppercase tracking-widest border border-dashed border-zinc-800 px-5 py-2.5 rounded bg-[#0A0D12]">
              sample video
            </span>

            {/* Bottom Overlay: Control Bar */}
            <div className="absolute bottom-0 left-0 right-0 h-16 bg-gradient-to-t from-black/95 to-transparent flex items-end justify-between px-4 py-3">
              <div className="flex items-center space-x-4 text-zinc-300">
                <button className="hover:text-white transition-colors"><Volume2 size={18} /></button>
              </div>
              <div className="flex items-center space-x-4 text-zinc-300">
                <span className="font-mono text-[10px] tracking-widest font-bold px-1.5 py-0.5 border border-[#2A2F38] rounded text-zinc-400 bg-black/50">1080P</span>
                <button className="hover:text-white transition-colors"><Settings size={18} /></button>
                <button className="hover:text-white transition-colors"><Maximize2 size={18} /></button>
              </div>
            </div>
          </div>

          {/* Right Pane (~30% Width) */}
          <div className="w-full md:w-72 bg-[#0D1117] p-5 flex flex-col justify-between">
            <div>
              <div className="flex items-start space-x-3 mb-6">
                <div className="w-10 h-10 rounded-full bg-[#161B22] border-2 border-[#00B8FC] flex items-center justify-center font-mono font-bold text-white text-xs flex-shrink-0">
                  AO
                </div>
                <div>
                  <h3 className="text-white font-bold font-sans text-sm leading-tight mb-1 truncate">
                    ARENA_OFFICIAL
                  </h3>
                  <p className="text-[#00B8FC] font-mono text-[10px] uppercase tracking-wider">TOURNAMENT FINALS</p>
                </div>
              </div>

              <div className="flex items-center space-x-2 mb-6">
                <Users size={14} className="text-[#FF2B56]" />
                <span className="text-white font-mono text-sm font-bold">24.8K</span>
                <span className="text-zinc-500 font-sans text-xs uppercase tracking-wider">viewers</span>
              </div>

              <div className="flex flex-wrap gap-2">
                <span className="bg-[#161B22] border border-[#2A2F38] text-zinc-300 font-mono text-[10px] px-2 py-1 rounded-full uppercase tracking-wider">GRAND FINALS</span>
                <span className="bg-[#161B22] border border-[#2A2F38] text-zinc-300 font-mono text-[10px] px-2 py-1 rounded-full uppercase tracking-wider">BO5</span>
                <span className="bg-[#161B22] border border-[#2A2F38] text-zinc-300 font-mono text-[10px] px-2 py-1 rounded-full uppercase tracking-wider">ESPORTS</span>
              </div>
            </div>

            <div className="mt-6">
              <button 
                className="w-full bg-[#00B8FC] hover:bg-[#009ADB] text-black font-bold uppercase text-xs tracking-wider py-2.5 rounded-sm transition-colors focus:outline-none focus:ring-2 focus:ring-[#00B8FC] focus:ring-offset-2 focus:ring-offset-[#0D1117]"
              >
                Enter Arena
              </button>
            </div>
          </div>
        </div>

        {/* Right Flanking Card (Preview) */}
        <div className="w-56 h-[340px] flex-shrink-0 hidden lg:flex items-center justify-center bg-[#0D1117] border border-[#2A2F38] rounded-md opacity-30 scale-90">
          <span className="text-xs font-mono text-zinc-600 uppercase tracking-widest">sample video</span>
        </div>

        {/* Navigation Buttons */}
        <button className="absolute left-2 z-30 bg-black/80 hover:bg-[#00B8FC] hover:text-black text-white p-2.5 border border-[#2A2F38] rounded-full transition-colors">
          <ChevronLeft size={24} />
        </button>
        <button className="absolute right-2 z-30 bg-black/80 hover:bg-[#00B8FC] hover:text-black text-white p-2.5 border border-[#2A2F38] rounded-full transition-colors">
          <ChevronRight size={24} />
        </button>
      </div>
      {/* Subtle bottom border */}
      <div className="absolute bottom-0 left-0 right-0 border-b border-[#2A2F38]"></div>
    </div>
  );
};

export default FeaturedMatchCarousel;
