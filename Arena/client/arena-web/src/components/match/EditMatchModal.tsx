import React, { useState } from 'react';
import { MatchResponse, UpdateMatchRequest, UpdateMatchStatusRequest } from '../../types';
import { matchService } from '../../api/matchService';
import Input from '../common/Input';
import Button from '../common/Button';
import { ArenaDatePicker } from '../common/ArenaDatePicker';

interface EditMatchModalProps {
  match: MatchResponse;
  onClose: () => void;
  onSave: (updatedMatch: MatchResponse) => void;
}

const toLocalIso = (utcString: string) => {
  const d = new Date(utcString);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  const h = String(d.getHours()).padStart(2, '0');
  const min = String(d.getMinutes()).padStart(2, '0');
  return `${y}-${m}-${day}T${h}:${min}`;
};

const EditMatchModal: React.FC<EditMatchModalProps> = ({ match, onClose, onSave }) => {
  const [teamAId, setTeamAId] = useState(match.teamAId.toString());
  const [teamBId, setTeamBId] = useState(match.teamBId.toString());
  const [scheduledTime, setScheduledTime] = useState(toLocalIso(match.scheduledTime));
  const [status, setStatus] = useState<MatchResponse['status']>(match.status);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError('');

    try {
      let updatedMatch = match;

      if (
        Number(teamAId) !== match.teamAId ||
        Number(teamBId) !== match.teamBId ||
        scheduledTime !== toLocalIso(match.scheduledTime)
      ) {
        const updateData: UpdateMatchRequest = {
          teamAId: Number(teamAId),
          teamBId: Number(teamBId),
          scheduledTime: new Date(scheduledTime).toISOString(),
        };
        updatedMatch = await matchService.updateMatch(match.matchId, updateData);
      }

      if (status !== match.status) {
        const statusData: UpdateMatchStatusRequest = {
          status,
          forceOverride: true,
        };
        updatedMatch = await matchService.updateMatchStatus(match.matchId, statusData);
      }

      onSave(updatedMatch);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update match');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center bg-black/75 backdrop-blur-sm p-4 overflow-y-auto pt-[10vh] pb-[10vh]">
      <div className="bg-arena-surface border border-arena-border rounded-[14px] p-8 w-full max-w-2xl shadow-[0_0_50px_rgba(0,184,252,0.05)] shrink-0 my-auto">
        <h2 className="text-2xl font-bold text-white mb-6">
          Edit Match
        </h2>
        
        {error && (
          <div className="mb-6 bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-[14px] text-sm text-center">
            {error}
          </div>
        )}
        
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-2 gap-6">
            <Input
              label="Team A"
              id="teamAId"
              type="number"
              min="1"
              value={teamAId}
              onChange={(e) => setTeamAId(e.target.value)}
              required
            />
            <Input
              label="Team B"
              id="teamBId"
              type="number"
              min="1"
              value={teamBId}
              onChange={(e) => setTeamBId(e.target.value)}
              required
            />
          </div>

          <div className="mb-4">
            <ArenaDatePicker
              label="Scheduled Start Time (Local)"
              value={new Date(scheduledTime)}
              onChange={(date) => {
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                setScheduledTime(`${year}-${month}-${day}T${hours}:${minutes}`);
              }}
            />
          </div>

          <div className="flex flex-col w-full mb-4">
            <label className="text-xs uppercase tracking-widest text-arena-textMuted mb-1 font-sans">
              Status
            </label>
            <div className="relative">
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value as MatchResponse['status'])}
                className="w-full bg-transparent border-b border-arena-border py-2 text-white font-sans text-sm focus:outline-none focus:border-arena-cyan transition-colors duration-200 appearance-none"
              >
                <option value="Scheduled" className="bg-arena-surface text-white">Scheduled</option>
                <option value="Live" className="bg-arena-surface text-white">Live</option>
                <option value="Ended" className="bg-arena-surface text-white">Ended</option>
                <option value="Cancelled" className="bg-arena-surface text-white">Cancelled</option>
              </select>
              {/* Custom arrow to replace native select arrow */}
              <div className="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none text-arena-textMuted">
                ▼
              </div>
              <div className="absolute bottom-0 left-0 h-[1px] w-full bg-arena-cyan scale-x-0 opacity-0 transition-all duration-300 peer-focus:scale-x-100 peer-focus:opacity-100 shadow-[0_0_10px_rgba(0,184,252,0.5)] pointer-events-none"></div>
            </div>
          </div>

          <div className="flex justify-end space-x-4 pt-6 mt-4 border-t border-arena-border">
            <Button
              type="button"
              variant="secondary"
              onClick={onClose}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={isSubmitting}
            >
              Save Changes
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default EditMatchModal;
