import { useState, useEffect } from 'react';
import { matchService } from '../api/matchService';
import { MatchResponse, StreamResponse } from '../types';

export type MatchStatusState = 'loading' | 'notFound' | 'error' | 'Scheduled' | 'Live' | 'Ended' | 'Cancelled';

interface UseMatchStatusResult {
  status: MatchStatusState;
  match: MatchResponse | null;
  stream: StreamResponse | null;
  error: any;
}

export function useMatchStatus(matchId: string | undefined): UseMatchStatusResult {
  const [state, setState] = useState<UseMatchStatusResult>({
    status: 'loading',
    match: null,
    stream: null,
    error: null,
  });

  useEffect(() => {
    if (!matchId) return;

    let intervalId: ReturnType<typeof setTimeout>;
    let isMounted = true;
    let errorCount = 0;
    const parsedId = parseInt(matchId);

    async function fetchData() {
      try {
        const matchData = await matchService.getMatch(parsedId);
        
        let streamData = null;
        try {
          streamData = await matchService.getMatchStream(parsedId);
        } catch (streamErr: any) {
          // It's normal for a match to not have a stream yet
        }

        if (isMounted) {
          errorCount = 0; // reset on success
          setState({
            status: matchData.status as MatchStatusState,
            match: matchData,
            stream: streamData,
            error: null,
          });

          // Stop polling if the match is in a terminal state
          if (matchData.status !== 'Ended' && matchData.status !== 'Cancelled') {
            intervalId = setTimeout(fetchData, 15000);
          }
        }
      } catch (err: any) {
        if (isMounted) {
          if (err.response?.status === 404) {
            setState((s) => ({ ...s, status: 'notFound' }));
          } else {
            errorCount++;
            setState((s) => ({ ...s, status: 'error', error: err }));
            const backoff = Math.min(15000 * Math.pow(2, errorCount - 1), 60000);
            intervalId = setTimeout(fetchData, backoff);
          }
        }
      }
    }

    // Initial fetch
    fetchData();

    return () => {
      isMounted = false;
      clearTimeout(intervalId);
    };
  }, [matchId]);

  return state;
}
