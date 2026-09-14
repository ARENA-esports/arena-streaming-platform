import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { StreamResponse, MatchResponse } from '../types';
import { matchService } from '../api/matchService';
import { StreamContainer } from '../components/player/StreamContainer';
import Badge from '../components/common/Badge';
import Button from '../components/common/Button';
import { useAuth } from '../context/AuthContext';
import EditMatchModal from '../components/match/EditMatchModal';
import DeleteMatchModal from '../components/match/DeleteMatchModal';
import BattleBar from '../components/match/BattleBar';
import TeamSelector from '../components/match/TeamSelector';
import FactionChat from '../components/chat/FactionChat';

export const MatchRoomView: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [match, setMatch] = useState<MatchResponse | null>(null);
  const [stream, setStream] = useState<StreamResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [, setIs404] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

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
        <div className="bg-arena-surface border border-arena-border border-dashed p-12 text-center rounded-[14px]">
          <h2 className="text-2xl font-bold text-white mb-2">Match Not Found</h2>
          <button onClick={() => navigate('/')} className="text-arena-cyan hover:underline font-semibold text-sm">Return Home</button>
        </div>
      </div>
    );
  }

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

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-4 w-full h-full py-4">
        {/* Video Player Section (Span 3 Columns) */}
        <div className="lg:col-span-3 flex flex-col gap-3 min-w-0">
          <BattleBar matchId={match.matchId} />
          {isLoading ? (
            <div className="w-full aspect-video bg-arena-surface border border-arena-border animate-pulse"></div>
          ) : (
            <StreamContainer apiChannelName={stream?.channelName} />
          )}
          <TeamSelector matchId={match.matchId} />
        </div>

        {/* Chat Sidebar (Span 1 Column) */}
        <div className="lg:col-span-1 h-[600px] lg:h-auto min-w-0">
          <FactionChat matchId={match.matchId} />
        </div>
      </div>

      {isEditModalOpen && (
        <EditMatchModal
          match={match}
          onClose={() => setIsEditModalOpen(false)}
          onSave={(updatedMatch) => {
            setMatch(updatedMatch);
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
