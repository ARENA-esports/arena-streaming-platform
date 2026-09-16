import React, { useState } from 'react';
import { tournamentService } from '../../api/tournamentService';
import { ArenaDatePicker } from '../common/ArenaDatePicker';
import { TournamentResponse } from '../../types';

interface EditTournamentModalProps {
  tournament: TournamentResponse;
  onClose: () => void;
  onSuccess: () => void;
}

const toLocalIso = (dateStr: string) => {
  const d = new Date(dateStr);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  const h = String(d.getHours()).padStart(2, '0');
  const min = String(d.getMinutes()).padStart(2, '0');
  return `${y}-${m}-${day}T${h}:${min}`;
};

export const EditTournamentModal: React.FC<EditTournamentModalProps> = ({ tournament, onClose, onSuccess }) => {
  const [name, setName] = useState(tournament.name);
  const [seasonIdentifier, setSeasonIdentifier] = useState(tournament.season_identifier);
  const [startDate, setStartDate] = useState(toLocalIso(tournament.start_date));
  const [endDate, setEndDate] = useState(toLocalIso(tournament.end_date));
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const [error, setError] = useState('');

  const canEdit = tournament.status !== 'Cancelled' && tournament.status !== 'Completed';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError('');

    try {
      await tournamentService.updateTournament(tournament.id, {
        name,
        season_identifier: seasonIdentifier,
        start_date: new Date(startDate).toISOString(),
        end_date: new Date(endDate).toISOString(),
      });
      onSuccess();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update tournament.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = async () => {
    if (!window.confirm(`Are you sure you want to cancel tournament '${tournament.name}'?`)) return;
    setIsCancelling(true);
    setError('');
    
    try {
      await tournamentService.cancelTournament(tournament.id);
      onSuccess();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to cancel tournament.');
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-start justify-center z-50 p-4 overflow-y-auto pt-[10vh] pb-[10vh]">
      <div className="bg-arena-surface border border-arena-border rounded-[14px] p-6 max-w-md w-full shadow-[0_0_50px_rgba(0,184,252,0.1)] shrink-0 my-auto">
        <div className="flex justify-between items-start mb-4">
          <h2 className="text-xl font-bold text-white">Edit Tournament</h2>
          {!canEdit && (
            <span className="px-2.5 py-1 text-[10px] font-bold uppercase tracking-wider rounded-[14px] bg-zinc-800 text-zinc-400 border border-zinc-700">
              {tournament.status}
            </span>
          )}
        </div>
        
        {error && <div className="mb-4 p-3 bg-red-950/60 border border-red-500/40 rounded-[14px] text-red-200 text-sm">{error}</div>}
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1">Name</label>
            <input 
              type="text" 
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full bg-black border border-arena-border rounded-[14px] px-4 py-2.5 focus:border-arena-cyan outline-none text-white text-sm transition-colors disabled:opacity-50"
              required
              disabled={!canEdit}
            />
          </div>
          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1">Season Identifier</label>
            <input 
              type="text" 
              value={seasonIdentifier}
              onChange={(e) => setSeasonIdentifier(e.target.value)}
              className="w-full bg-black border border-arena-border rounded-[14px] px-4 py-2.5 focus:border-arena-cyan outline-none text-white text-sm transition-colors disabled:opacity-50"
              required
              disabled={!canEdit}
            />
          </div>
          <div className="pt-2">
            <ArenaDatePicker
              label="START DATE (LOCAL)"
              value={new Date(startDate)}
              onChange={(date) => {
                if (!canEdit) return;
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                setStartDate(`${year}-${month}-${day}T${hours}:${minutes}`);
              }}
            />
          </div>
          <div className="pt-2">
            <ArenaDatePicker
              label="END DATE (LOCAL)"
              value={new Date(endDate)}
              onChange={(date) => {
                if (!canEdit) return;
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                setEndDate(`${year}-${month}-${day}T${hours}:${minutes}`);
              }}
            />
          </div>
          
          <div className="flex justify-between items-center pt-6 border-t border-arena-border mt-6">
            {canEdit ? (
              <button 
                type="button"
                onClick={handleCancel}
                disabled={isCancelling}
                className="text-red-400 hover:text-red-300 font-bold px-3 py-1.5 rounded-[14px] text-xs hover:bg-red-500/10 transition-colors disabled:opacity-50"
              >
                {isCancelling ? 'Cancelling...' : 'Cancel Tournament'}
              </button>
            ) : (
              <div></div>
            )}
            
            <div className="flex gap-3">
              <button 
                type="button"
                onClick={onClose}
                className="text-arena-textMuted hover:text-white transition-colors px-4 py-2 rounded-[14px] text-xs font-semibold"
              >
                Close
              </button>
              {canEdit && (
                <button 
                  type="submit"
                  disabled={isSubmitting || !name || !seasonIdentifier}
                  className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2.5 px-6 rounded-[14px] transition-colors disabled:opacity-50 text-xs uppercase tracking-wider"
                >
                  {isSubmitting ? 'Saving...' : 'Save'}
                </button>
              )}
            </div>
          </div>
        </form>
      </div>
    </div>
  );
};
