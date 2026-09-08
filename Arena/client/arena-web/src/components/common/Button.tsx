import { FC, ButtonHTMLAttributes } from 'react';
import { twMerge } from 'tailwind-merge';
import { clsx, ClassValue } from 'clsx';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const Button: FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  className,
  disabled,
  ...props
}) => {
  const baseStyles = 'inline-flex items-center justify-center rounded-[14px] font-bold uppercase transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-arena-bg';
  
  const variants: Record<NonNullable<ButtonProps['variant']>, string> = {
    primary: 'bg-arena-cyan text-black hover:bg-arena-cyanHover active:bg-arena-cyanActive focus:ring-arena-cyan',
    secondary: 'bg-transparent border border-arena-border text-white hover:border-arena-cyan focus:ring-arena-cyan',
    danger: 'bg-transparent border border-arena-crimson text-arena-crimson hover:bg-arena-crimson hover:text-white focus:ring-arena-crimson',
  };

  const sizes: Record<NonNullable<ButtonProps['size']>, string> = {
    sm: 'px-3 py-1.5 text-xs',
    md: 'px-6 py-2.5 text-sm tracking-wider',
    lg: 'px-8 py-3 text-base tracking-widest',
  };

  const classes = cn(
    baseStyles,
    variants[variant as NonNullable<ButtonProps['variant']>],
    sizes[size as NonNullable<ButtonProps['size']>],
    (disabled || isLoading) && 'opacity-50 cursor-not-allowed',
    className
  );

  return (
    <button className={classes} disabled={disabled || isLoading} {...props}>
      {isLoading ? (
        <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-current" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      ) : null}
      {children}
    </button>
  );
};

export default Button;
