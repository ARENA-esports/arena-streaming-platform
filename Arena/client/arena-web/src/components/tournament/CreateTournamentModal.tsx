import React, { useState } from 'react';
import { tournamentService } from '../../api/tournamentService';
import { ArenaDatePicker } from '../common/ArenaDatePicker';

interface CreateTournamentModalProps {
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

export const CreateTournamentModal: React.FC<CreateTournamentModalProps> = ({ onClose, onSuccess }) => {
  const [name, setName] = useState('');
  const [seasonIdentifier, setSeasonIdentifier] = useState('');
  const [startDate, setStartDate] = useState(toLocalIso(new Date().toISOString()));
  
  // Default end date is tomorrow
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const [endDate, setEndDate] = useState(toLocalIso(tomorrow.toISOString()));
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError('');

    try {
      await tournamentService.createTournament({
        name,
        season_identifier: seasonIdentifier,
        start_date: new Date(startDate).toISOString(),
        end_date: new Date(endDate).toISOString(),
      });
      onSuccess();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create tournament.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-start justify-center z-50 p-4 overflow-y-auto pt-[10vh] pb-[10vh]">
      <div className="bg-arena-surface border border-arena-border rounded-[14px] p-6 max-w-md w-full shadow-[0_0_50px_rgba(0,184,252,0.1)] shrink-0 my-auto">
        <h2 className="text-xl font-bold text-white mb-4">Create Tournament</h2>
        {error && <div className="mb-4 p-3 bg-red-950/60 border border-red-500/40 rounded-[14px] text-red-200 text-sm">{error}</div>}
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1">Name</label>
            <input 
              type="text" 
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full bg-black border border-arena-border rounded-[14px] px-4 py-2.5 focus:border-arena-cyan outline-none text-white text-sm transition-colors"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1">Season Identifier</label>
            <input 
              type="text" 
              value={seasonIdentifier}
              onChange={(e) => setSeasonIdentifier(e.target.value)}
              placeholder="e.g. S1-2026"
              className="w-full bg-black border border-arena-border rounded-[14px] px-4 py-2.5 focus:border-arena-cyan outline-none text-white text-sm transition-colors"
              required
            />
          </div>
          <div className="pt-2">
            <ArenaDatePicker
              label="START DATE (LOCAL)"
              value={new Date(startDate)}
              onChange={(date) => {
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
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                setEndDate(`${year}-${month}-${day}T${hours}:${minutes}`);
              }}
            />
          </div>
          <div className="flex justify-end gap-3 pt-6 border-t border-arena-border mt-6">
            <button 
              type="button"
              onClick={onClose}
              className="text-arena-textMuted hover:text-white transition-colors px-4 py-2 rounded-[14px] text-xs font-semibold"
            >
              Cancel
            </button>
            <button 
              type="submit"
              disabled={isSubmitting || !name || !seasonIdentifier}
              className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2.5 px-6 rounded-[14px] transition-colors disabled:opacity-50 text-xs uppercase tracking-wider"
            >
              {isSubmitting ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
