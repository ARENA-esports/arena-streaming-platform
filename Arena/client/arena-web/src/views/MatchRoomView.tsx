import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { useMatchStatus } from '../hooks/useMatchStatus';
import { useTwitchPlayback } from '../hooks/useTwitchPlayback';
import { useWatchHeartbeat } from '../hooks/useWatchHeartbeat';
import { StreamContainer } from '../components/player/StreamContainer';
import Badge from '../components/common/Badge';
import WalletBalance from '../components/common/WalletBalance';
import Button from '../components/common/Button';
import { useAuth } from '../context/AuthContext';
import { useWallet } from '../context/WalletContext';
import { useNotification } from '../context/NotificationContext';
import EditMatchModal from '../components/match/EditMatchModal';
import DeleteMatchModal from '../components/match/DeleteMatchModal';
import WeaponShop from '../components/match/WeaponShop';
import TeamSelector from '../components/match/TeamSelector';
import FactionChat from '../components/chat/FactionChat';
import { ScheduledView } from '../components/match/ScheduledView';
import { EndedView } from '../components/match/EndedView';
import { CancelledView } from '../components/match/CancelledView';
import WatchRewardStatus from '../components/match/WatchRewardStatus';

export const MatchRoomView: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { updateBalance } = useWallet();
  const { notify } = useNotification();
  const hasNotifiedCapRef = useRef(false);

  const { status, match, stream, error, refetch } = useMatchStatus(matchId);
  const { isPlaying, onPlay, onPause, resetPlayback } = useTwitchPlayback();

  // Reset playback state if the match status leaves 'Live' or match changes
  useEffect(() => {
    if (status !== 'Live') {
      resetPlayback();
      hasNotifiedCapRef.current = false;
    }
  }, [status, matchId, resetPlayback]);

  // Heartbeat is active for authenticated users watching an active Live match with a valid stream
  const isViewer = Boolean(user);
  const isLiveMatch = status === 'Live' && Boolean(match);
  const streamId = stream?.streamId ?? stream?.id;
  const hasValidStream = typeof streamId === 'number' && streamId > 0;
  const isHeartbeatEligible = isViewer && isLiveMatch && hasValidStream && isPlaying;

  useWatchHeartbeat({
    streamId,
    isPlaying: isHeartbeatEligible,
    onSuccess: (response) => {
      console.log('[Arena Heartbeat] Watch tick successful! Balance:', response.currentBalance);
      if (response.success && response.currentBalance !== undefined) {
        updateBalance(response.currentBalance);
        hasNotifiedCapRef.current = false;
      }
    },
    onError: (err: unknown) => {
      console.warn('[Arena Heartbeat] Watch tick error:', err);
      if (axios.isAxiosError(err) && err.response) {
        const { status, data, headers } = err.response;
        const retryAfter = headers?.['retry-after'] ?? headers?.['Retry-After'];
        const remainingSeconds = data?.remainingSeconds;
        const message = data?.message;

        if (status === 429) {
          // SCRUM-114: Minimum interval anti-farm rejection -> silent
          if (retryAfter !== undefined || (typeof remainingSeconds === 'number' && remainingSeconds > 0)) {
            return;
          }

          // SCRUM-115: Coin cap reached -> debounced informational notification
          const isCapMessage = typeof message === 'string' && message.toLowerCase().includes('cap');
          if (isCapMessage || (remainingSeconds === undefined && retryAfter === undefined)) {
            if (!hasNotifiedCapRef.current) {
              hasNotifiedCapRef.current = true;
              notify(message || 'Coin cap reached for this stream window.', 'info');
            }
            return;
          }
        } else if (status === 400) {
          // SCRUM-118: Stream not live rejection -> informational notification
          if (message === 'Stream is not currently live.') {
            notify(message, 'info');
            return;
          }

          // Other 400 errors (e.g. invalid stream ID)
          console.warn('Watch tick rejected:', message);
          return;
        }
      }
    }
  });

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('team');
  const [selectedTeamId, setSelectedTeamId] = useState<number | null>(null);
  const [teamsData, setTeamsData] = useState<{ teamA: any; teamB: any } | null>(null);
  const [activeChannel, setActiveChannel] = useState<string>('');

  const handleTeamsLoaded = useCallback((teamA: any, teamB: any) => {
    setTeamsData({ teamA, teamB });
  }, []);

  const teamsMap = teamsData
    ? {
        [teamsData.teamA.teamId]: {
          name: teamsData.teamA.name,
          color: teamsData.teamA.colorHex || '#EF4444',
        },
        [teamsData.teamB.teamId]: {
          name: teamsData.teamB.name,
          color: teamsData.teamB.colorHex || '#00B8FC',
        },
      }
    : undefined;

  if (status === 'loading') {
    return (
      <div className="flex justify-center py-20">
        <div className="w-8 h-8 border-4 border-arena-cyan border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  if (status === 'notFound') {
    return (
      <div className="max-w-7xl mx-auto px-4 py-8">
        <div className="bg-arena-surface border border-arena-border border-dashed p-12 text-center rounded-[14px]">
          <h2 className="text-2xl font-bold text-white mb-2">Match Not Found</h2>
          <button onClick={() => navigate('/')} className="text-arena-cyan hover:underline font-semibold text-sm">Return Home</button>
        </div>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="max-w-7xl mx-auto px-4 py-8">
        <div className="bg-arena-surface border border-arena-crimson/50 p-12 text-center rounded-[14px]">
          <h2 className="text-2xl font-bold text-white mb-2">Error Loading Match</h2>
          <p className="text-arena-textMuted mb-4">{error?.message || 'An unexpected error occurred.'}</p>
          <button onClick={() => window.location.reload()} className="text-arena-cyan hover:underline font-semibold text-sm">Retry</button>
        </div>
      </div>
    );
  }

  if (!match) return null;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">
            Match Overview
          </h1>
          <div className="flex items-center space-x-4">
            <Badge status={match.status} />
            {match.status === 'Live' ? (
              <>
                <span className="text-sm text-arena-cyan font-mono flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-arena-crimson animate-pulse" />
                  Live Broadcast
                </span>
                <WatchRewardStatus
                  isViewer={isViewer}
                  isLiveMatch={isLiveMatch}
                  hasValidStream={hasValidStream}
                  isPlaying={isPlaying}
                />
              </>
            ) : match.status === 'Ended' ? (
              <span className="text-sm text-arena-textMuted font-mono">
                Match Ended
              </span>
            ) : match.status === 'Cancelled' ? (
              <span className="text-sm text-arena-crimson font-mono">
                Match Cancelled
              </span>
            ) : (
              <span className="text-sm text-arena-textMuted font-mono">
                Scheduled: {new Date(match.scheduledTime).toLocaleString()}
              </span>
            )}
          </div>
        </div>

        {user?.role === 'Organizer' && (
          <div className="flex items-center space-x-4">
            <Button
              variant="secondary"
              size="md"
              onClick={() => setIsEditModalOpen(true)}
            >
              EDIT
            </Button>
            <Button
              variant="danger"
              size="md"
              onClick={() => setIsDeleteModalOpen(true)}
            >
              DELETE
            </Button>
          </div>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[3fr_1fr] gap-4 w-full py-4">
        {/* Video Player Section - Dynamic tile swapping */}
        <div className="lg:col-start-1 flex flex-col gap-3 min-w-0">
          {status === 'Scheduled' && <ScheduledView match={match} />}
          {status === 'Live' && (
            <StreamContainer
              apiChannelName={stream?.channelName}
              isPlaying={isPlaying}
              onPlay={onPlay}
              onPause={onPause}
              onChannelChange={setActiveChannel}
            />
          )}
          {status === 'Ended' && <EndedView match={match} />}
          {status === 'Cancelled' && <CancelledView match={match} />}
        </div>

        {/* Panel Group - Always mounted in right column, height matched to stream */}
        <div className="lg:col-start-2 flex flex-col gap-3 min-w-0 h-full">
          <div className="flex items-center justify-end shrink-0">
            <WalletBalance />
          </div>

          {/* Tab switcher - mobile only */}
          <div className="flex lg:hidden border-b border-arena-border mb-2 shrink-0">
            {['team', 'chat', 'battle'].map(tab => (
              <button
                key={tab}
                onClick={() => {
                  setActiveTab(tab);
                  if (tab === 'battle') {
                    document.getElementById('weapon-shop-section')?.scrollIntoView({ behavior: 'smooth' });
                  }
                }}
                className={`px-4 py-3 flex-1 text-sm font-bold uppercase tracking-wider transition-colors duration-200 ${activeTab === tab
                    ? 'border-b-2 border-arena-cyan text-arena-text'
                    : 'text-arena-textMuted hover:text-arena-text'
                  }`}
              >
                {tab}
              </button>
            ))}
          </div>

          <div className={`shrink-0 ${activeTab === 'team' ? 'block' : 'hidden lg:block'}`}>
            <TeamSelector
              matchId={match.matchId}
              selectedTeamId={selectedTeamId}
              onTeamSelect={setSelectedTeamId}
              onTeamsLoaded={handleTeamsLoaded}
            />
          </div>
          <div className={`flex-1 min-h-[320px] min-w-0 ${activeTab === 'chat' ? 'flex flex-col' : 'hidden lg:flex lg:flex-col'}`}>
            <FactionChat
              matchId={match.matchId}
              teamAId={match.teamAId}
              teamBId={match.teamBId}
              selectedTeamId={selectedTeamId}
              teamsMap={teamsMap}
              onSelectTeam={setSelectedTeamId}
              activeChannel={activeChannel}
            />
          </div>
        </div>
      </div>

      {/* Full-Width Weapon Shop across the whole display below stream & chat */}
      <div id="weapon-shop-section" className="w-full pb-8">
        <WeaponShop matchId={match.matchId} teamId={selectedTeamId} />
      </div>

      {isEditModalOpen && (
        <EditMatchModal
          match={match}
          onClose={() => setIsEditModalOpen(false)}
          onSave={() => {
            setIsEditModalOpen(false);
            refetch?.();
          }}
        />
      )}

      {isDeleteModalOpen && (
        <DeleteMatchModal
          match={match}
          onClose={() => setIsDeleteModalOpen(false)}
          onDelete={() => {
            setIsDeleteModalOpen(false);
            navigate('/matches');
          }}
        />
      )}
    </div>
  );
};

export default MatchRoomView;
