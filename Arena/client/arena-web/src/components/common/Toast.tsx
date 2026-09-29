import React from 'react';
import { useNotification, NotificationType } from '../../context/NotificationContext';

// ─── Icon helpers ─────────────────────────────────────────────────────────────

const icons: Record<NotificationType, React.ReactNode> = {
  error: (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <circle cx="8" cy="8" r="7.25" stroke="currentColor" strokeWidth="1.5" />
      <path d="M8 4.5V8.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <circle cx="8" cy="11" r="0.75" fill="currentColor" />
    </svg>
  ),
  success: (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <circle cx="8" cy="8" r="7.25" stroke="currentColor" strokeWidth="1.5" />
      <path d="M5 8L7 10L11 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  ),
  info: (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <circle cx="8" cy="8" r="7.25" stroke="currentColor" strokeWidth="1.5" />
      <path d="M8 11.5V7.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      <circle cx="8" cy="5" r="0.75" fill="currentColor" />
    </svg>
  ),
};

// ─── Colour tokens per type ───────────────────────────────────────────────────
// Uses existing CSS custom properties from index.css

const typeStyles: Record<NotificationType, { container: string; icon: string }> = {
  error: {
    container: 'bg-[var(--panel)] border border-[var(--live)]/70 text-[var(--text)]',
    icon: 'text-[var(--live)]',
  },
  success: {
    container: 'bg-[var(--panel)] border border-[var(--prime)]/70 text-[var(--text)]',
    icon: 'text-[var(--prime)]',
  },
  info: {
    container: 'bg-[var(--panel)] border border-[var(--line)] text-[var(--text)]',
    icon: 'text-[var(--subtext)]',
  },
};

// ─── ToastList (renders all active notifications) ─────────────────────────────

export const ToastList: React.FC = () => {
  const { notifications, dismiss } = useNotification();

  if (notifications.length === 0) return null;

  return (
    <div
      aria-live="polite"
      aria-label="Notifications"
      style={{
        position: 'fixed',
        bottom: '1.25rem',
        right: '1rem',
        zIndex: 1000,
        display: 'flex',
        flexDirection: 'column',
        gap: '0.5rem',
        maxWidth: 'min(360px, calc(100vw - 2rem))',
        width: '100%',
        pointerEvents: 'none',
      }}
    >
      {notifications.map(n => (
        <div
          key={n.id}
          role="alert"
          data-testid="toast"
          data-notification-type={n.type}
          className={`${typeStyles[n.type].container} flex items-start gap-3 px-4 py-3 rounded-[10px] shadow-lg text-sm`}
          style={{ pointerEvents: 'all' }}
        >
          {/* Icon */}
          <span className={`${typeStyles[n.type].icon} flex-shrink-0 mt-[1px]`}>
            {icons[n.type]}
          </span>

          {/* Message */}
          <span className="flex-1 leading-snug break-words">
            {n.message}
          </span>

          {/* Dismiss button */}
          <button
            aria-label="Dismiss notification"
            onClick={() => dismiss(n.id)}
            className="flex-shrink-0 text-[var(--muted)] hover:text-[var(--text)] transition-colors ml-1 leading-none"
            style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: '1rem', lineHeight: 1 }}
          >
            ×
          </button>
        </div>
      ))}
    </div>
  );
};

export default ToastList;
