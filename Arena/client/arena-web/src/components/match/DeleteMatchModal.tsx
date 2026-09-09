import React, { useState } from 'react';
import { MatchResponse } from '../../types';
import { matchService } from '../../api/matchService';
import Button from '../common/Button';
import Badge from '../common/Badge';

interface DeleteMatchModalProps {
  match: MatchResponse;
  onClose: () => void;
  onDelete: () => void;
}

const DeleteMatchModal: React.FC<DeleteMatchModalProps> = ({ match, onClose, onDelete }) => {
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState('');

  const handleDelete = async () => {
    setIsDeleting(true);
    setError('');
    try {
      await matchService.deleteMatch(match.matchId);
      onDelete();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete match');
      setIsDeleting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm">
      <div className="bg-arena-surface border border-arena-border rounded-[14px] p-8 w-full max-w-lg shadow-[0_0_50px_rgba(255,59,48,0.1)]">
        <h2 className="text-2xl font-bold text-arena-crimson mb-6">
          Confirm Deletion
        </h2>
        
        {error && (
          <div className="mb-6 bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-[14px] text-sm text-center">
            {error}
          </div>
        )}

        <div className="mb-8">
          <p className="text-arena-textMuted mb-4">
            Are you sure you want to permanently delete this match? This action cannot be undone.
          </p>
          
          <div className="bg-arena-bg border border-arena-border p-4 rounded-[14px]">
            <div className="text-lg font-bold text-white mb-2">
              Team {match.teamAId} <span className="text-arena-textMuted italic mx-1">VS</span> Team {match.teamBId}
            </div>
            <div className="flex items-center space-x-4">
              <Badge status={match.status} />
              <span className="text-xs text-arena-textMuted font-mono">
                {new Date(match.scheduledTime).toLocaleString()}
              </span>
            </div>
          </div>
        </div>

        <div className="flex justify-end space-x-4 border-t border-arena-border pt-6">
          <Button
            type="button"
            variant="secondary"
            onClick={onClose}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="danger"
            isLoading={isDeleting}
            onClick={handleDelete}
          >
            Delete Match
          </Button>
        </div>
      </div>
    </div>
  );
};

export default DeleteMatchModal;
