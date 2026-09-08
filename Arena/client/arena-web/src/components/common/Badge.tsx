import { FC } from 'react';
import { twMerge } from 'tailwind-merge';
import { clsx, ClassValue } from 'clsx';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

interface BadgeProps {
  status: 'Scheduled' | 'Live' | 'Ended';
  className?: string;
}

export const Badge: FC<BadgeProps> = ({ status, className }) => {
  if (status === 'Live') {
    return (
      <div className={cn("inline-flex items-center px-2.5 py-0.5 rounded-full bg-arena-surface border border-arena-crimson text-arena-crimson font-bold text-xs uppercase tracking-widest shadow-[0_0_10px_rgba(255,43,86,0.3)]", className)}>
        <span className="w-2 h-2 rounded-full bg-arena-crimson mr-1.5 animate-pulse shadow-[0_0_5px_#FF2B56]"></span>
        LIVE
      </div>
    );
  }

  if (status === 'Ended') {
    return (
      <div className={cn("inline-flex items-center px-2.5 py-0.5 rounded-full bg-arena-surface border border-arena-border text-arena-textMuted font-bold text-xs uppercase tracking-widest", className)}>
        ENDED
      </div>
    );
  }

  return (
    <div className={cn("inline-flex items-center px-2.5 py-0.5 rounded-full bg-arena-surface border border-arena-cyan text-arena-cyan font-bold text-xs uppercase tracking-widest", className)}>
      SCHEDULED
    </div>
  );
};

export default Badge;
