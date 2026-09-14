import React, { useState } from 'react';
import { ChevronLeft, ChevronRight, Calendar as CalendarIcon } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';

export const ScheduleHeader: React.FC = () => {
  const { user } = useAuth();

  const [dayOffset, setDayOffset] = useState(0);

  const startOfWeek = new Date();
  startOfWeek.setDate(startOfWeek.getDate() + dayOffset);

  const endOfWeek = new Date(startOfWeek);
  endOfWeek.setDate(startOfWeek.getDate() + 6);

  const formatDate = (d: Date) => d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  return (
    <div className="w-full flex flex-col items-start gap-6 mb-8">
      <h2 className="text-xl md:text-2xl font-bold text-[var(--text)] tracking-tight">
        No upcoming matches scheduled for {user?.username || 'stream_by_arena'}.
      </h2>

      <div className="flex items-center gap-3">
        <button
          onClick={() => setDayOffset(0)}
          className="px-4 py-1.5 rounded-full text-xs font-bold uppercase transition-colors bg-[#00B8FC] text-black hover:bg-[#0096D6]"
        >
          Today
        </button>

        <div className="flex items-center gap-1">
          <button
            onClick={() => { if (dayOffset >= 7) setDayOffset(prev => prev - 7) }}
            disabled={dayOffset === 0}
            className={`rounded-full p-1.5 transition-colors border ${dayOffset === 0
                ? 'bg-transparent border-transparent text-[var(--muted)] opacity-50 cursor-not-allowed'
                : 'bg-[var(--panel-2)] border-[var(--line)] text-[var(--text)] hover:border-[#00B8FC]'
              }`}
          >
            <ChevronLeft size={16} />
          </button>
          <button
            onClick={() => setDayOffset(prev => prev + 7)}
            className="bg-[var(--panel-2)] border border-[var(--line)] text-[var(--text)] rounded-full p-1.5 hover:border-[#00B8FC] transition-colors"
          >
            <ChevronRight size={16} />
          </button>
        </div>

        <button className="flex items-center gap-2 bg-[var(--panel-2)] border border-[var(--line)] text-[var(--text)] hover:border-[#00B8FC] px-3 py-1.5 rounded-full text-sm font-mono transition-colors">
          <CalendarIcon size={14} className="text-[#00B8FC]" />
          <span>{formatDate(startOfWeek)} – {formatDate(endOfWeek)}</span>
        </button>
      </div>
    </div>
  );
};
