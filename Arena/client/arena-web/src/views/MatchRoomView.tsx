import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMatchStatus } from '../hooks/useMatchStatus';
import { StreamContainer } from '../components/player/StreamContainer';
import Badge from '../components/common/Badge';
import Button from '../components/common/Button';
import { useAuth } from '../context/AuthContext';
import EditMatchModal from '../components/match/EditMatchModal';
import DeleteMatchModal from '../components/match/DeleteMatchModal';
import BattleBar from '../components/match/BattleBar';
import TeamSelector from '../components/match/TeamSelector';
import FactionChat from '../components/chat/FactionChat';
import { ScheduledView } from '../components/match/ScheduledView';
import { EndedView } from '../components/match/EndedView';
import { CancelledView } from '../components/match/CancelledView';

export const MatchRoomView: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const { status, match, stream, error } = useMatchStatus(matchId);
  
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('team');

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
            <span className="text-sm text-arena-textMuted font-mono">
              Scheduled: {new Date(match.scheduledTime).toLocaleString()}
            </span>
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

      <div className="grid grid-cols-1 lg:grid-cols-[3fr_1fr] gap-4 w-full h-full py-4">
        {/* Video Player Section - Dynamic tile swapping */}
        <div className="lg:col-start-1 flex flex-col gap-3 min-w-0">
          {status === 'Scheduled' && <ScheduledView match={match} />}
          {status === 'Live' && <StreamContainer apiChannelName={stream?.channelName} />}
          {status === 'Ended' && <EndedView match={match} />}
          {status === 'Cancelled' && <CancelledView match={match} />}
        </div>

        {/* Panel Group - Always mounted in right column, visibility toggled on mobile */}
        <div className="lg:col-start-2 flex flex-col gap-4 min-w-0">
          {/* Tab switcher - mobile only */}
          <div className="flex lg:hidden border-b border-arena-border mb-2">
            {['team', 'chat', 'battle'].map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`px-4 py-3 flex-1 text-sm font-bold uppercase tracking-wider transition-colors duration-200 ${
                  activeTab === tab 
                    ? 'border-b-2 border-arena-cyan text-arena-text' 
                    : 'text-arena-textMuted hover:text-arena-text'
                }`}
              >
                {tab}
              </button>
            ))}
          </div>

          <div className={activeTab === 'team' ? 'block' : 'hidden lg:block'}>
            <TeamSelector matchId={match.matchId} />
          </div>
          <div className={`h-[600px] lg:h-auto min-w-0 ${activeTab === 'chat' ? 'block' : 'hidden lg:block'}`}>
            <FactionChat matchId={match.matchId} />
          </div>
          <div className={activeTab === 'battle' ? 'block' : 'hidden lg:block'}>
            <BattleBar matchId={match.matchId} />
          </div>
        </div>
      </div>

      {isEditModalOpen && (
        <EditMatchModal
          match={match}
          onClose={() => setIsEditModalOpen(false)}
          onSave={() => {
            // Note: The parent component won't re-render immediately until the next poll.
            // If immediate UI update is desired, we could manually update the state here, 
            // but for simplicity and consistency with polling, we'll let the next poll catch it.
            // A more robust approach might be to mutate the local state if the hook exposed a mutate function.
            setIsEditModalOpen(false);
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
