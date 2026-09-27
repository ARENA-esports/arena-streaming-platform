import React from 'react';
import { render, screen } from '@testing-library/react';
import WatchRewardStatus from '../src/components/match/WatchRewardStatus';

describe('WatchRewardStatus', () => {
  describe('Explicit Status Prop', () => {
    it('renders Earning Coins state', () => {
      render(<WatchRewardStatus status="earning" />);
      expect(screen.getByText('Earning Coins')).toBeInTheDocument();
      const badge = screen.getByTestId('watch-reward-status');
      expect(badge).toHaveAttribute('data-status', 'earning');
    });

    it('renders Rewards Paused state', () => {
      render(<WatchRewardStatus status="paused" />);
      expect(screen.getByText('Rewards Paused')).toBeInTheDocument();
      const badge = screen.getByTestId('watch-reward-status');
      expect(badge).toHaveAttribute('data-status', 'paused');
    });

    it('renders Tab inactive state', () => {
      render(<WatchRewardStatus status="tab-inactive" />);
      expect(screen.getByText('Rewards Paused / Tab Inactive')).toBeInTheDocument();
      const badge = screen.getByTestId('watch-reward-status');
      expect(badge).toHaveAttribute('data-status', 'tab-inactive');
    });

    it('renders Reward Cap Reached state', () => {
      render(<WatchRewardStatus status="capped" />);
      expect(screen.getByText('Reward Cap Reached')).toBeInTheDocument();
      const badge = screen.getByTestId('watch-reward-status');
      expect(badge).toHaveAttribute('data-status', 'capped');
    });

    it('renders Rewards Inactive state', () => {
      render(<WatchRewardStatus status="inactive" />);
      expect(screen.getByText('Rewards Inactive')).toBeInTheDocument();
      const badge = screen.getByTestId('watch-reward-status');
      expect(badge).toHaveAttribute('data-status', 'inactive');
    });
  });

  describe('Dynamic Boolean Flag State Resolution', () => {
    it('resolves to Earning Coins when all conditions are met', () => {
      render(
        <WatchRewardStatus
          isViewer={true}
          isLiveMatch={true}
          hasValidStream={true}
          isPlaying={true}
          isDocumentVisible={true}
          isCapped={false}
          isStreamNotLive={false}
        />
      );
      expect(screen.getByText('Earning Coins')).toBeInTheDocument();
      expect(screen.getByTestId('watch-reward-status')).toHaveAttribute('data-status', 'earning');
    });

    it('resolves to Rewards Paused when playback is paused', () => {
      render(
        <WatchRewardStatus
          isViewer={true}
          isLiveMatch={true}
          hasValidStream={true}
          isPlaying={false}
          isDocumentVisible={true}
          isCapped={false}
        />
      );
      expect(screen.getByText('Rewards Paused')).toBeInTheDocument();
      expect(screen.getByTestId('watch-reward-status')).toHaveAttribute('data-status', 'paused');
    });

    it('resolves to Tab inactive when document is hidden', () => {
      render(
        <WatchRewardStatus
          isViewer={true}
          isLiveMatch={true}
          hasValidStream={true}
          isPlaying={true}
          isDocumentVisible={false}
          isCapped={false}
        />
      );
      expect(screen.getByText(/Tab Inactive/i)).toBeInTheDocument();
      expect(screen.getByTestId('watch-reward-status')).toHaveAttribute('data-status', 'tab-inactive');
    });

    it('resolves to Reward Cap Reached when capped is true', () => {
      render(
        <WatchRewardStatus
          isViewer={true}
          isLiveMatch={true}
          hasValidStream={true}
          isPlaying={true}
          isDocumentVisible={true}
          isCapped={true}
        />
      );
      expect(screen.getByText('Reward Cap Reached')).toBeInTheDocument();
      expect(screen.getByTestId('watch-reward-status')).toHaveAttribute('data-status', 'capped');
    });

    it('resolves to Rewards Inactive when match is not live or user is not viewer', () => {
      const { rerender } = render(
        <WatchRewardStatus
          isViewer={false}
          isLiveMatch={true}
          hasValidStream={true}
          isPlaying={true}
        />
      );
      expect(screen.getByText('Rewards Inactive')).toBeInTheDocument();
      expect(screen.getByTestId('watch-reward-status')).toHaveAttribute('data-status', 'inactive');

      rerender(
        <WatchRewardStatus
          isViewer={true}
          isLiveMatch={false}
          hasValidStream={true}
          isPlaying={true}
        />
      );
      expect(screen.getByText('Rewards Inactive')).toBeInTheDocument();

      rerender(
        <WatchRewardStatus
          isViewer={true}
          isLiveMatch={true}
          hasValidStream={true}
          isPlaying={true}
          isStreamNotLive={true}
        />
      );
      expect(screen.getByText('Rewards Inactive')).toBeInTheDocument();
    });
  });
});
