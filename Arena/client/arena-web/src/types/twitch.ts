export interface TwitchPlayerInstance {
  addEventListener(event: string, callback: () => void): void;
  removeEventListener(event: string, callback: () => void): void;
  play(): void;
  pause(): void;
  setChannel(channel: string): void;
  getChannel(): string;
  isPaused(): boolean;
  destroy?: () => void;
}

export interface TwitchPlayerOptions {
  channel?: string;
  video?: string;
  collection?: string;
  width?: string | number;
  height?: string | number;
  parent?: string[];
  autoplay?: boolean;
  muted?: boolean;
  time?: string;
}

export interface TwitchPlayerConstructor {
  new (element: string | HTMLElement, options: TwitchPlayerOptions): TwitchPlayerInstance;
  PLAY: string;
  PAUSE: string;
  READY: string;
  ONLINE: string;
  OFFLINE: string;
  ENDED: string;
}

export interface TwitchSdk {
  Player: TwitchPlayerConstructor;
}

declare global {
  interface Window {
    Twitch?: TwitchSdk;
  }
}
