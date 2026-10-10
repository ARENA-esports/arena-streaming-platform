import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import ViewerEngagementDashboardView, { formatWatchTime, formatLastEventAt } from '../src/views/ViewerEngagementDashboardView';
import { analyticsService } from '../src/api/analyticsService';
import { StreamEngagementResponse } from '../src/types';

jest.mock('../src/api/analyticsService', () => ({
  analyticsService: {
    getStreamEngagement: jest.fn(),
  },
}));

const mockGetStreamEngagement = analyticsService.getStreamEngagement as jest.MockedFunction<
  typeof analyticsService.getStreamEngagement
>;

describe('ViewerEngagementDashboardView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('formatWatchTime helper', () => {
    it('formats seconds under 1 minute', () => {
      expect(formatWatchTime(45)).toBe('45s');
      expect(formatWatchTime(0)).toBe('0s');
      expect(formatWatchTime(-5)).toBe('0s');
    });

    it('formats minutes and seconds under 1 hour', () => {
      expect(formatWatchTime(750)).toBe('12m 30s');
      expect(formatWatchTime(60)).toBe('1m 0s');
    });

    it('formats hours and minutes for 1 hour or more', () => {
      expect(formatWatchTime(3600)).toBe('1h 0m');
      expect(formatWatchTime(7200)).toBe('2h 0m');
      expect(formatWatchTime(8100)).toBe('2h 15m');
      expect(formatWatchTime(10800)).toBe('3h 0m');
    });
  });

  describe('formatLastEventAt helper', () => {
    it('handles null or empty timestamps gracefully', () => {
      expect(formatLastEventAt(null)).toBe('No activity');
      expect(formatLastEventAt('')).toBe('No activity');
    });

    it('handles invalid date strings gracefully', () => {
      expect(formatLastEventAt('not-a-date')).toBe('No activity');
    });

    it('formats valid ISO date strings', () => {
      const formatted = formatLastEventAt('2026-10-05T10:30:00Z');
      expect(formatted).not.toBe('No activity');
      expect(formatted).not.toContain('Invalid Date');
    });
  });

  it('Test 1 — displays loading state while the API request is pending', () => {
    // Return a promise that does not resolve immediately
    mockGetStreamEngagement.mockReturnValue(new Promise(() => {}));

    render(<ViewerEngagementDashboardView />);

    expect(screen.getByText('Loading engagement analytics...')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('Test 2 — renders successful stream engagement data with prominent metrics', async () => {
    const mockStreams: StreamEngagementResponse[] = [
      {
        streamId: 101,
        totalWatchSeconds: 7200,
        totalCoinsEarned: 1200,
        totalWatchTicks: 120,
        uniqueViewers: 25,
        lastEventAt: '2026-10-05T10:30:00Z',
      },
      {
        streamId: 102,
        totalWatchSeconds: 3600,
        totalCoinsEarned: 600,
        totalWatchTicks: 60,
        uniqueViewers: 15,
        lastEventAt: null,
      },
    ];

    mockGetStreamEngagement.mockResolvedValueOnce(mockStreams);

    render(<ViewerEngagementDashboardView />);

    // Wait for data load
    await waitFor(() => {
      expect(screen.queryByText('Loading engagement analytics...')).not.toBeInTheDocument();
    });

    // Verify stream IDs appear
    expect(screen.getByText('Stream 101')).toBeInTheDocument();
    expect(screen.getByText('Stream 102')).toBeInTheDocument();

    // Verify formatted watch time per stream appears
    expect(screen.getByText('2h 0m')).toBeInTheDocument();
    expect(screen.getByText('1h 0m')).toBeInTheDocument();

    // Verify coins earned per stream appear
    expect(screen.getByText('1,200 coins')).toBeInTheDocument();
    expect(screen.getByText('600 coins')).toBeInTheDocument();

    // Verify watch ticks appear
    expect(screen.getByText('120 ticks')).toBeInTheDocument();
    expect(screen.getByText('60 ticks')).toBeInTheDocument();

    // Verify unique viewers appear
    expect(screen.getByText('25 viewers')).toBeInTheDocument();
    expect(screen.getByText('15 viewers')).toBeInTheDocument();

    // Verify null lastEventAt renders as "No activity"
    expect(screen.getByText('No activity')).toBeInTheDocument();
  });

  it('Test 3 — calculates aggregate KPI summary values correctly', async () => {
    const mockStreams: StreamEngagementResponse[] = [
      {
        streamId: 101,
        totalWatchSeconds: 7200,
        totalCoinsEarned: 1200,
        totalWatchTicks: 120,
        uniqueViewers: 25,
        lastEventAt: '2026-10-05T10:30:00Z',
      },
      {
        streamId: 102,
        totalWatchSeconds: 3600,
        totalCoinsEarned: 800,
        totalWatchTicks: 60,
        uniqueViewers: 15,
        lastEventAt: '2026-10-05T11:00:00Z',
      },
    ];

    mockGetStreamEngagement.mockResolvedValueOnce(mockStreams);

    render(<ViewerEngagementDashboardView />);

    await waitFor(() => {
      expect(screen.queryByText('Loading engagement analytics...')).not.toBeInTheDocument();
    });

    // KPI 1: Total Watch Time = 7200 + 3600 = 10800s => 3h 0m
    expect(screen.getByTitle('Total Watch Time')).toHaveTextContent('3h 0m');

    // KPI 2: Total Coins Earned = 1200 + 800 = 2,000
    expect(screen.getByTitle('Total Coins Earned')).toHaveTextContent('2,000');

    // KPI 3: Tracked Streams = 2
    expect(screen.getByTitle('Tracked Streams')).toHaveTextContent('2');

    // KPI 4: Total Stream Viewers = 25 + 15 = 40
    expect(screen.getByTitle('Total Stream Viewers')).toHaveTextContent('40');
  });

  it('Test 4 — displays empty state when API returns no stream data', async () => {
    mockGetStreamEngagement.mockResolvedValueOnce([]);

    render(<ViewerEngagementDashboardView />);

    await waitFor(() => {
      expect(screen.queryByText('Loading engagement analytics...')).not.toBeInTheDocument();
    });

    expect(screen.getByText('No viewer engagement data yet')).toBeInTheDocument();
    expect(
      screen.getByText('Engagement metrics will appear here once streams receive viewer activity.')
    ).toBeInTheDocument();
  });

  it('Test 5 — displays user-friendly error state with retry button on API failure', async () => {
    mockGetStreamEngagement.mockRejectedValueOnce(new Error('Internal Server Error'));

    render(<ViewerEngagementDashboardView />);

    await waitFor(() => {
      expect(screen.queryByText('Loading engagement analytics...')).not.toBeInTheDocument();
    });

    expect(screen.getByText('Unable to Load Analytics')).toBeInTheDocument();
    expect(
      screen.getByText('Failed to load viewer engagement analytics. Please try again.')
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('Test 6 — retries API request and recovers on click of Retry button', async () => {
    // First request fails
    mockGetStreamEngagement.mockRejectedValueOnce(new Error('Network error'));

    render(<ViewerEngagementDashboardView />);

    await waitFor(() => {
      expect(screen.getByText('Unable to Load Analytics')).toBeInTheDocument();
    });

    // Second request succeeds
    const recoveredStreams: StreamEngagementResponse[] = [
      {
        streamId: 205,
        totalWatchSeconds: 1800,
        totalCoinsEarned: 350,
        totalWatchTicks: 30,
        uniqueViewers: 8,
        lastEventAt: '2026-10-05T12:00:00Z',
      },
    ];
    mockGetStreamEngagement.mockResolvedValueOnce(recoveredStreams);

    const retryButton = screen.getByRole('button', { name: /retry/i });
    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(screen.getByText('Stream 205')).toBeInTheDocument();
    });

    expect(screen.getByText('350 coins')).toBeInTheDocument();
    expect(screen.queryByText('Unable to Load Analytics')).not.toBeInTheDocument();
  });
});
