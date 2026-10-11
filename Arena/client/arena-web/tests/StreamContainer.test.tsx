import React from 'react';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { StreamContainer } from '../src/components/player/StreamContainer';

// Mock TwitchPlayer child component to simulate player events
jest.mock('../src/components/player/TwitchPlayer', () => ({
  TwitchPlayer: ({
    channel,
    onPlay,
    onPause,
  }: {
    channel: string;
    onPlay?: () => void;
    onPause?: () => void;
  }) => (
    <div data-testid="mock-twitch-player" data-channel={channel}>
      <button data-testid="player-play" onClick={onPlay}>Player Play</button>
      <button data-testid="player-pause" onClick={onPause}>Player Pause</button>
    </div>
  ),
}));

describe('StreamContainer — Playback Eligibility Lifecycle', () => {
  it('does not invoke onPlay on mount and invalidates playback state', () => {
    const onPlay = jest.fn();
    const onPause = jest.fn();

    render(
      <StreamContainer
        apiChannelName="Arena_streams"
        onPlay={onPlay}
        onPause={onPause}
      />
    );

    // Must NOT call onPlay on mount
    expect(onPlay).not.toHaveBeenCalled();
    // Must call onPause to ensure state is clean/idle
    expect(onPause).toHaveBeenCalled();
  });

  it('forwards genuine play event from player to onPlay', () => {
    const onPlay = jest.fn();
    const onPause = jest.fn();

    render(
      <StreamContainer
        apiChannelName="Arena_streams"
        onPlay={onPlay}
        onPause={onPause}
      />
    );

    fireEvent.click(screen.getByTestId('player-play'));
    expect(onPlay).toHaveBeenCalledTimes(1);
  });

  it('forwards pause event to onPause for standard channels', () => {
    const onPlay = jest.fn();
    const onPause = jest.fn();

    render(
      <StreamContainer
        apiChannelName="Arena_streams"
        onPlay={onPlay}
        onPause={onPause}
      />
    );

    onPause.mockClear();
    fireEvent.click(screen.getByTestId('player-pause'));
    expect(onPause).toHaveBeenCalledTimes(1);
  });

  it('forwards pause event to onPause for mock or empty channels without bypass', () => {
    const onPlay = jest.fn();
    const onPause = jest.fn();

    // Mock channel
    const { rerender } = render(
      <StreamContainer
        apiChannelName="mock_channel_123"
        onPlay={onPlay}
        onPause={onPause}
      />
    );

    onPause.mockClear();
    fireEvent.click(screen.getByTestId('player-pause'));
    expect(onPause).toHaveBeenCalledTimes(1);

    // Empty channel
    rerender(
      <StreamContainer
        apiChannelName=""
        onPlay={onPlay}
        onPause={onPause}
      />
    );

    onPause.mockClear();
    fireEvent.click(screen.getByTestId('player-pause'));
    expect(onPause).toHaveBeenCalledTimes(1);
  });

  it('invalidates old playback state when switching channels via presets', () => {
    const onPlay = jest.fn();
    const onPause = jest.fn();
    const onChannelChange = jest.fn();

    render(
      <StreamContainer
        apiChannelName="Arena_streams"
        onPlay={onPlay}
        onPause={onPause}
        onChannelChange={onChannelChange}
      />
    );

    onPause.mockClear();
    onPlay.mockClear();

    // Click preset button
    const presetBtn = screen.getByText('riotgames');
    fireEvent.click(presetBtn);

    // Must invalidate playback on channel switch
    expect(onPause).toHaveBeenCalled();
    // Must NOT call onPlay before player plays
    expect(onPlay).not.toHaveBeenCalled();
    expect(onChannelChange).toHaveBeenCalledWith('riotgames');
  });

  it('invalidates old playback state when loading channel via input', () => {
    const onPlay = jest.fn();
    const onPause = jest.fn();
    const onChannelChange = jest.fn();

    render(
      <StreamContainer
        apiChannelName="Arena_streams"
        onPlay={onPlay}
        onPause={onPause}
        onChannelChange={onChannelChange}
      />
    );

    onPause.mockClear();
    onPlay.mockClear();

    const input = screen.getByPlaceholderText('Type live Twitch channel...');
    fireEvent.change(input, { target: { value: 'esl_dota2' } });

    const loadBtn = screen.getByText('Load Stream');
    fireEvent.click(loadBtn);

    expect(onPause).toHaveBeenCalled();
    expect(onPlay).not.toHaveBeenCalled();
    expect(onChannelChange).toHaveBeenCalledWith('esl_dota2');
  });
});
