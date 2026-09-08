import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { StreamResponse, MatchResponse } from '../types';
import { matchService } from '../api/matchService';
import TwitchEmbed from '../components/player/TwitchEmbed';
import FallbackAlert from '../components/player/FallbackAlert';
import Badge from '../components/common/Badge';

export const MatchRoomView: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const [match, setMatch] = useState<MatchResponse | null>(null);
  const [stream, setStream] = useState<StreamResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [is404, setIs404] = useState(false);

  useEffect(() => {
    if (!matchId) return;

    const fetchRoomData = async () => {
      try {
        const matchData = await matchService.getMatch(parseInt(matchId));
        setMatch(matchData);

        try {
          const streamData = await matchService.getMatchStream(parseInt(matchId));
          setStream(streamData);
        } catch (streamErr: any) {
          if (streamErr.response?.status === 404) {
            setIs404(true);
          }
        }
      } catch (err) {
        console.error('Failed to load match', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchRoomData();
  }, [matchId]);

  if (isLoading) {
    return (
      <div className="flex justify-center py-20">
        <div className="w-8 h-8 border-4 border-arena-cyan border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  if (!match) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-8">
        <div className="bg-arena-surface border border-arena-border border-dashed p-12 text-center rounded-sm">
          <h2 className="text-2xl font-display font-bold text-white mb-2 uppercase">Match Not Found</h2>
          <button onClick={() => navigate('/')} className="text-arena-cyan hover:underline uppercase text-sm font-bold tracking-widest">Return Home</button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-display font-black text-white tracking-widest uppercase mb-2">
            Team {match.teamAId} <span className="text-arena-textMuted italic mx-2">VS</span> Team {match.teamBId}
          </h1>
          <div className="flex items-center space-x-4">
            <Badge status={match.status} />
            <span className="text-sm text-arena-textMuted font-mono">
              Scheduled: {new Date(match.scheduledStartTime).toLocaleString()}
            </span>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2">
          {stream ? (
            <TwitchEmbed url={stream.twitchUrl} />
          ) : is404 ? (
            <FallbackAlert />
          ) : (
            <div className="w-full aspect-video bg-arena-surface border border-arena-border animate-pulse"></div>
          )}
        </div>
        
        <div className="bg-arena-surface border border-arena-border p-6 rounded-sm">
          <h3 className="text-lg font-display font-bold text-white tracking-widest uppercase border-b border-arena-border pb-4 mb-4">
            Match Details
          </h3>
          <div className="space-y-4">
            <div>
              <p className="text-xs text-arena-textMuted uppercase tracking-widest mb-1">Status</p>
              <p className="font-bold text-white uppercase">{match.status}</p>
            </div>
            {stream && (
              <>
                <div>
                  <p className="text-xs text-arena-textMuted uppercase tracking-widest mb-1">Broadcaster</p>
                  <p className="font-bold text-arena-cyan">{stream.channelName}</p>
                </div>
                {stream.startedAt && (
                  <div>
                    <p className="text-xs text-arena-textMuted uppercase tracking-widest mb-1">Started At</p>
                    <p className="font-mono text-sm text-white">{new Date(stream.startedAt).toLocaleString()}</p>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default MatchRoomView;
