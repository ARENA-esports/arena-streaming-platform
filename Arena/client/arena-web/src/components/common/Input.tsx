import { InputHTMLAttributes, forwardRef } from 'react';
import { twMerge } from 'tailwind-merge';
import { clsx, ClassValue } from 'clsx';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, className, id, ...props }, ref) => {
    const inputId = id || label.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className="flex flex-col w-full mb-4">
        <label
          htmlFor={inputId}
          className="text-xs uppercase tracking-widest text-arena-textMuted mb-1 font-sans"
        >
          {label}
        </label>
        <div className="relative">
          <input
            id={inputId}
            ref={ref}
            className={cn(
              "w-full bg-transparent border-b border-arena-border py-2 text-white font-sans text-sm focus:outline-none focus:border-arena-cyan transition-colors duration-200 placeholder-arena-textMuted/50",
              error && "border-arena-crimson focus:border-arena-crimson",
              className
            )}
            {...props}
          />
          {/* Subtle bottom ambient cyan glow on focus */}
          <div className="absolute bottom-0 left-0 h-[1px] w-full bg-arena-cyan scale-x-0 opacity-0 transition-all duration-300 peer-focus:scale-x-100 peer-focus:opacity-100 shadow-[0_0_10px_rgba(0,184,252,0.5)] pointer-events-none"></div>
        </div>
        {error && (
          <p className="mt-1 text-xs text-arena-crimson font-sans">{error}</p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

export default Input;
