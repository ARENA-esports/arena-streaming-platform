import { useState, useEffect } from 'react';
import { TwitchSdk } from '../types/twitch';

const TWITCH_PLAYER_SDK_URL = 'https://player.twitch.tv/js/embed/v1.js';
const SCRIPT_ID = 'twitch-player-sdk';

type SdkState = 'idle' | 'loading' | 'ready' | 'error';

export function useTwitchSdk(): { sdk: TwitchSdk | null; state: SdkState } {
  const [state, setState] = useState<SdkState>(() => {
    // If already loaded from a previous mount, skip re-loading
    if (typeof window !== 'undefined' && window.Twitch?.Player) {
      return 'ready';
    }
    return 'idle';
  });

  const [sdk, setSdk] = useState<TwitchSdk | null>(() => {
    if (typeof window !== 'undefined' && window.Twitch?.Player) {
      return window.Twitch;
    }
    return null;
  });

  useEffect(() => {
    // Already ready (set in state initializer or a previous mount)
    if (state === 'ready') {
      return;
    }

    // Script already inserted by a prior mount — wait for it to load
    const existingScript = document.getElementById(SCRIPT_ID) as HTMLScriptElement | null;

    if (existingScript) {
      setState('loading');
      const onLoad = () => {
        if (window.Twitch?.Player) {
          setSdk(window.Twitch);
          setState('ready');
        } else {
          setState('error');
        }
      };
      const onError = () => setState('error');
      existingScript.addEventListener('load', onLoad);
      existingScript.addEventListener('error', onError);
      return () => {
        existingScript.removeEventListener('load', onLoad);
        existingScript.removeEventListener('error', onError);
      };
    }

    // First mount — create and inject the script tag
    setState('loading');
    const script = document.createElement('script');
    script.id = SCRIPT_ID;
    script.src = TWITCH_PLAYER_SDK_URL;
    script.async = true;

    script.onload = () => {
      if (window.Twitch?.Player) {
        setSdk(window.Twitch);
        setState('ready');
      } else {
        setState('error');
      }
    };
    script.onerror = () => setState('error');

    document.head.appendChild(script);

    // Do not remove the script on cleanup: other mounted players may still need it.
    // The guard at the top of the effect handles the "already loaded" case cleanly.
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return { sdk, state };
}
