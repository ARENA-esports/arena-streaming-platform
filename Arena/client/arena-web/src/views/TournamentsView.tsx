import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { tournamentService } from '../api/tournamentService';
import { TournamentResponse } from '../types';
import { CreateTournamentModal } from '../components/tournament/CreateTournamentModal';
import { EditTournamentModal } from '../components/tournament/EditTournamentModal';

export const TournamentsView: React.FC = () => {
  const { user } = useAuth();
  const [tournaments, setTournaments] = useState<TournamentResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingTournament, setEditingTournament] = useState<TournamentResponse | null>(null);

  const fetchTournaments = async () => {
    setLoading(true);
    try {
      const data = await tournamentService.getAllTournaments();
      setTournaments(data);
      setError('');
    } catch (err) {
      setError('Failed to load tournaments.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTournaments();
  }, []);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active': return 'text-emerald-400 bg-emerald-500/10 border border-emerald-500/30';
      case 'Scheduled': return 'text-arena-cyan bg-arena-cyan/10 border border-arena-cyan/30';
      case 'Completed': return 'text-zinc-400 bg-zinc-800/50 border border-zinc-700/50';
      case 'Cancelled': return 'text-rose-400 bg-rose-500/10 border border-rose-500/30';
      default: return 'text-zinc-300 bg-zinc-800/40 border border-zinc-700/40';
    }
  };

  if (loading) {
    return <div className="p-8 text-arena-text">Loading tournaments...</div>;
  }

  return (
    <div className="max-w-5xl mx-auto p-6 text-arena-text">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-bold text-white tracking-tight">Tournaments</h1>
          <p className="text-sm text-arena-textMuted mt-1">Manage and track live esports tournament series</p>
        </div>
        {user?.role === 'Organizer' && (
          <button 
            onClick={() => setIsCreateModalOpen(true)}
            className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2 px-5 rounded-[14px] shadow-[0_0_15px_rgba(0,184,252,0.3)] transition-all shrink-0 uppercase tracking-wider text-xs"
          >
            Create Tournament
          </button>
        )}
      </div>
      
      {error && <div className="mb-6 p-4 bg-red-950/60 border border-red-500/40 rounded-[14px] text-red-200 text-sm">{error}</div>}

      <div className="space-y-4">
        {tournaments.length === 0 ? (
          <div className="bg-arena-surface border border-arena-border rounded-[14px] p-12 text-center shadow-lg">
            <p className="text-arena-textMuted mb-2">No tournaments found.</p>
            {user?.role === 'Organizer' && (
              <p className="text-sm text-arena-textMuted">Click the Create Tournament button above to get started.</p>
            )}
          </div>
        ) : (
          tournaments.map((t) => (
            <div key={t.id} className="bg-arena-surface border border-arena-border rounded-[14px] p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4 hover:border-arena-cyan/40 transition-all shadow-md">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-1.5">
                  <h2 className="text-xl font-bold text-white">{t.name}</h2>
                  <span className={`px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider rounded-[14px] ${getStatusColor(t.status)}`}>
                    {t.status}
                  </span>
                </div>
                <div className="text-xs font-mono text-arena-cyan mb-2 font-semibold">
                  {t.season_identifier}
                </div>
                <div className="text-xs text-arena-textMuted font-mono">
                  {new Date(t.start_date).toLocaleDateString()} - {new Date(t.end_date).toLocaleDateString()}
                </div>
              </div>
              
              {user?.role === 'Organizer' && (
                <button 
                  onClick={() => setEditingTournament(t)}
                  className="bg-[var(--panel-2)] hover:bg-zinc-800 text-white border border-arena-border hover:border-arena-cyan font-bold py-2 px-5 rounded-[14px] transition-all text-xs shrink-0"
                >
                  Edit
                </button>
              )}
            </div>
          ))
        )}
      </div>

      {isCreateModalOpen && (
        <CreateTournamentModal 
          onClose={() => setIsCreateModalOpen(false)}
          onSuccess={() => {
            setIsCreateModalOpen(false);
            fetchTournaments();
          }}
        />
      )}

      {editingTournament && (
        <EditTournamentModal 
          tournament={editingTournament}
          onClose={() => setEditingTournament(null)}
          onSuccess={() => {
            setEditingTournament(null);
            fetchTournaments();
          }}
        />
      )}
    </div>
  );
};

export default TournamentsView;
