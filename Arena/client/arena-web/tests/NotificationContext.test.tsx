import React from 'react';
import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { NotificationProvider, useNotification } from '../src/context/NotificationContext';
import { ToastList } from '../src/components/common/Toast';

// ─── Test harness component ───────────────────────────────────────────────────

const Harness: React.FC<{ onMount?: (ctx: ReturnType<typeof useNotification>) => void }> = ({ onMount }) => {
  const ctx = useNotification();

  React.useEffect(() => {
    onMount?.(ctx);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <button
        data-testid="notify-error"
        onClick={() => ctx.notify('Something went wrong', 'error')}
      >
        Trigger Error
      </button>
      <button
        data-testid="notify-success"
        onClick={() => ctx.notify('Purchase complete', 'success')}
      >
        Trigger Success
      </button>
      <button
        data-testid="notify-info"
        onClick={() => ctx.notify('Balance updated', 'info')}
      >
        Trigger Info
      </button>
      <ToastList />
    </div>
  );
};

// ─── Helper ───────────────────────────────────────────────────────────────────

const renderWithProvider = (ui: React.ReactNode = <Harness />) =>
  render(<NotificationProvider>{ui}</NotificationProvider>);

// ─── Tests ────────────────────────────────────────────────────────────────────

describe('NotificationContext + ToastList', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('throws when useNotification is used outside a provider', () => {
    // Suppress React error boundary output
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    const BadComponent = () => {
      useNotification();
      return null;
    };

    expect(() => render(<BadComponent />)).toThrow(
      'useNotification must be used within a NotificationProvider'
    );

    consoleSpy.mockRestore();
  });

  it('renders nothing when no notifications are active', () => {
    renderWithProvider();
    expect(screen.queryByTestId('toast')).not.toBeInTheDocument();
  });

  it('displays an error notification with the supplied message', () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-error'));

    const toast = screen.getByTestId('toast');
    expect(toast).toBeInTheDocument();
    expect(toast).toHaveTextContent('Something went wrong');
    expect(toast).toHaveAttribute('data-notification-type', 'error');
  });

  it('displays a success notification', () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-success'));

    const toast = screen.getByTestId('toast');
    expect(toast).toHaveTextContent('Purchase complete');
    expect(toast).toHaveAttribute('data-notification-type', 'success');
  });

  it('displays an info notification', () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-info'));

    const toast = screen.getByTestId('toast');
    expect(toast).toHaveTextContent('Balance updated');
    expect(toast).toHaveAttribute('data-notification-type', 'info');
  });

  it('dismisses a notification when the dismiss button is clicked', () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-error'));
    expect(screen.getByTestId('toast')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss notification' }));

    expect(screen.queryByTestId('toast')).not.toBeInTheDocument();
  });

  it('auto-dismisses the notification after 5 seconds', async () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-error'));
    expect(screen.getByTestId('toast')).toBeInTheDocument();

    act(() => {
      jest.advanceTimersByTime(5000);
    });

    await waitFor(() => {
      expect(screen.queryByTestId('toast')).not.toBeInTheDocument();
    });
  });

  it('stacks multiple notifications without breaking the UI', () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-error'));
    fireEvent.click(screen.getByTestId('notify-success'));
    fireEvent.click(screen.getByTestId('notify-info'));

    const toasts = screen.getAllByTestId('toast');
    expect(toasts).toHaveLength(3);
    expect(toasts[0]).toHaveAttribute('data-notification-type', 'error');
    expect(toasts[1]).toHaveAttribute('data-notification-type', 'success');
    expect(toasts[2]).toHaveAttribute('data-notification-type', 'info');
  });

  it('dismissing one notification does not remove others', () => {
    renderWithProvider();

    fireEvent.click(screen.getByTestId('notify-error'));
    fireEvent.click(screen.getByTestId('notify-success'));

    const dismissButtons = screen.getAllByRole('button', { name: 'Dismiss notification' });
    expect(dismissButtons).toHaveLength(2);

    // Dismiss the first one
    fireEvent.click(dismissButtons[0]);

    const remaining = screen.getAllByTestId('toast');
    expect(remaining).toHaveLength(1);
    expect(remaining[0]).toHaveAttribute('data-notification-type', 'success');
  });

  it('can be mounted inside existing application test setups', () => {
    // Simulate the typical usage inside an already-wrapped component tree
    let capturedCtx: ReturnType<typeof useNotification> | null = null;

    render(
      <NotificationProvider>
        <Harness
          onMount={ctx => {
            capturedCtx = ctx;
          }}
        />
      </NotificationProvider>
    );

    expect(capturedCtx).not.toBeNull();
    expect(typeof capturedCtx!.notify).toBe('function');
    expect(typeof capturedCtx!.dismiss).toBe('function');
    expect(Array.isArray(capturedCtx!.notifications)).toBe(true);
  });
});
