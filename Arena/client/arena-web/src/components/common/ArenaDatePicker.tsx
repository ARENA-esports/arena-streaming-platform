import React, { useState, useRef, useEffect } from 'react';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, Clock } from 'lucide-react';

interface ArenaDatePickerProps {
  value: Date;
  onChange: (date: Date) => void;
  label?: string;
}

export const ArenaDatePicker: React.FC<ArenaDatePickerProps> = ({
  value,
  onChange,
  label = "SCHEDULED START TIME (LOCAL)"
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [viewDate, setViewDate] = useState(new Date(value));
  const containerRef = useRef<HTMLDivElement>(null);

  // Close on outside click
  useEffect(() => {
    const handleOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleOutside);
    return () => document.removeEventListener('mousedown', handleOutside);
  }, []);

  const monthNames = [
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December"
  ];
  const daysOfWeek = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

  // Calendar calculations
  const year = viewDate.getFullYear();
  const month = viewDate.getMonth();
  const firstDayIndex = (new Date(year, month, 1).getDay() + 6) % 7; // Monday-indexed
  const daysInMonth = new Date(year, month + 1, 0).getDate();

  const handlePrevMonth = () => setViewDate(new Date(year, month - 1, 1));
  const handleNextMonth = () => setViewDate(new Date(year, month + 1, 1));

  const handleSelectDay = (day: number) => {
    const newDate = new Date(value);
    newDate.setFullYear(year, month, day);
    onChange(newDate);
  };

  const handleTimeChange = (hours: number, minutes: number) => {
    const newDate = new Date(value);
    newDate.setHours(hours);
    newDate.setMinutes(minutes);
    onChange(newDate);
  };

  return (
    <div className="relative w-full" ref={containerRef}>
      <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-[var(--muted)] mb-1">
        {label}
      </label>

      {/* Input Trigger Button */}
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="w-full flex items-center justify-between bg-[var(--panel-2)] border border-[var(--line)] hover:border-[var(--prime)] focus:border-[#00B8FC] text-[var(--text)] rounded-[14px] px-3.5 py-2.5 text-sm outline-none transition-all shadow-sm"
      >
        <span className="font-mono text-xs font-semibold">
          {value.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
          {' • '}
          {value.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true })}
        </span>
        <CalendarIcon className="text-[var(--prime)]" size={16}/>
      </button>

      {/* Popover Calendar Dropdown */}
      {isOpen && (
        <div className="absolute top-full mt-2 left-0 z-50 w-80 bg-[var(--panel)] border border-[var(--line)] rounded-[14px] p-4 shadow-2xl animate-in fade-in zoom-in-95 duration-150">
          
          {/* Month / Year Navigator */}
          <div className="flex items-center justify-between mb-4">
            <span className="font-bold text-sm text-[var(--text)] font-mono">
              {monthNames[month]} {year}
            </span>
            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={handlePrevMonth}
                className="p-1 rounded-[14px] hover:bg-[var(--panel-2)] text-[var(--subtext)] hover:text-white"
              >
                <ChevronLeft size={16}/>
              </button>
              <button
                type="button"
                onClick={handleNextMonth}
                className="p-1 rounded-[14px] hover:bg-[var(--panel-2)] text-[var(--subtext)] hover:text-white"
              >
                <ChevronRight size={16}/>
              </button>
            </div>
          </div>

          {/* Days of Week Header */}
          <div className="grid grid-cols-7 gap-1 text-center mb-2">
            {daysOfWeek.map((d) => (
              <span key={d} className="text-[10px] font-mono text-[var(--muted)] uppercase font-semibold">
                {d}
              </span>
            ))}
          </div>

          {/* Days Grid */}
          <div className="grid grid-cols-7 gap-1 text-center">
            {Array.from({ length: firstDayIndex }).map((_, i) => (
              <div key={`empty-${i}`} className="h-8" />
            ))}

            {Array.from({ length: daysInMonth }).map((_, i) => {
              const day = i + 1;
              const isSelected =
                value.getDate() === day &&
                value.getMonth() === month &&
                value.getFullYear() === year;

              return (
                <button
                  key={day}
                  type="button"
                  onClick={() => handleSelectDay(day)}
                  className={`h-8 w-8 mx-auto rounded-full text-xs font-mono transition-all flex items-center justify-center ${
                    isSelected
                      ? 'bg-[#00B8FC] text-black font-bold ring-2 ring-white shadow-[0_0_10px_rgba(0,184,252,0.5)]'
                      : 'text-[var(--text)] hover:bg-[var(--panel-2)] hover:text-[#00B8FC]'
                  }`}
                >
                  {day}
                </button>
              );
            })}
          </div>

          {/* Time Picker Bar */}
          <div className="mt-4 pt-3 border-t border-[var(--line)] flex items-center justify-between">
            <div className="flex items-center gap-2 text-xs font-mono text-[var(--muted)]">
              <Clock className="text-[#00B8FC]" size={14}/>
              <span>TIME:</span>
            </div>
            <input
              type="time"
              value={`${String(value.getHours()).padStart(2, '0')}:${String(value.getMinutes()).padStart(2, '0')}`}
              onChange={(e) => {
                const [h, m] = e.target.value.split(':').map(Number);
                if (!isNaN(h) && !isNaN(m)) handleTimeChange(h, m);
              }}
              className="bg-[var(--panel-2)] border border-[var(--line)] focus:border-[#00B8FC] text-[var(--text)] font-mono text-xs rounded-[14px] px-2.5 py-1 outline-none"
            />
          </div>

          <button
            type="button"
            onClick={() => setIsOpen(false)}
            className="w-full mt-3 py-2 bg-[#00B8FC] hover:bg-[#0096D6] text-black font-bold text-xs uppercase font-mono rounded-[14px] tracking-wider transition-all shadow-[0_0_12px_rgba(0,184,252,0.3)]"
          >
            Apply Date & Time
          </button>
        </div>
      )}
    </div>
  );
};
