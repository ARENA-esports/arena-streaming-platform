import React, { useState, useEffect } from 'react';
import { teamService, Team, formatLogoUrl } from '../api/teamService';
import { useAuth } from '../context/AuthContext';
import { CreateTeamModal } from '../components/team/CreateTeamModal';
import { TeamRosterModal } from '../components/team/TeamRosterModal';
import { Shield, Plus, Users, Search, ChevronRight, Sparkles, Camera } from 'lucide-react';

const TeamBadgeBox: React.FC<{ team: Team; isOrganizer: boolean; onLogoUpdated: () => void }> = ({ team, isOrganizer, onLogoUpdated }) => {
  const [imgError, setImgError] = useState(false);
  const [uploading, setUploading] = useState(false);
  const formattedUrl = formatLogoUrl(team.logoUrl);
  const isInvalidUrl = !formattedUrl || formattedUrl.includes('assets.arena.gg');
  const showImg = !isInvalidUrl && !imgError;

  useEffect(() => {
    setImgError(false);
  }, [team.logoUrl]);

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      await teamService.uploadLogo(team.teamId, file);
      setImgError(false);
      onLogoUpdated();
    } catch (err) {
      console.error('Failed to upload team logo', err);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div
      className="w-14 h-14 rounded-[14px] border-2 flex items-center justify-center font-bold text-base shrink-0 overflow-hidden shadow-sm relative group/badge"
      style={{ borderColor: team.colorHex, backgroundColor: `${team.colorHex}15` }}
    >
      {showImg ? (
        <img 
          src={formattedUrl} 
          alt={team.name} 
          className="w-full h-full object-cover" 
          onError={() => setImgError(true)}
        />
      ) : (
        <span style={{ color: team.colorHex }} className="font-bold">
          {(team.name || 'TM').substring(0, 2).toUpperCase()}
        </span>
      )}

      {isOrganizer && (
        <label 
          className="absolute inset-0 bg-black/60 opacity-0 group-hover/badge:opacity-100 flex items-center justify-center cursor-pointer transition-opacity text-white"
          title="Upload Team Logo"
        >
          <Camera size={18} />
          <input type="file" accept="image/*" onChange={handleFileUpload} className="hidden" disabled={uploading} />
        </label>
      )}
    </div>
  );
};

export const TeamsView: React.FC = () => {
  const { user } = useAuth();
  const isOrganizer = user?.role === 'Organizer';

  const [teams, setTeams] = useState<Team[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchQuery, setSearchQuery] = useState('');

  // Modals
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [selectedTeamId, setSelectedTeamId] = useState<number | null>(null);

  const fetchTeams = async () => {
    setLoading(true);
    try {
      const data = await teamService.getAllTeams();
      setTeams(data);
      setError('');
    } catch (err: any) {
      setError('Failed to load registered teams.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTeams();
  }, []);

  const filteredTeams = teams.filter((t) => {
    const nameStr = t.name || (t as any).team_name || (t as any).teamName || '';
    const idStr = (t.teamId ?? (t as any).team_id ?? (t as any).id ?? '').toString();
    return (
      nameStr.toLowerCase().includes(searchQuery.toLowerCase()) ||
      idStr.includes(searchQuery)
    );
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 text-arena-text">
      {/* Header Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-8 pb-6 border-b border-arena-border">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <span className="px-3 py-1 bg-arena-cyan/10 border border-arena-cyan/30 text-arena-cyan font-mono font-bold text-xs uppercase rounded-[14px] flex items-center gap-1.5">
              <Sparkles size={14} /> Esports Roster Hub
            </span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight font-display">
            Team & Roster Management
          </h1>
          <p className="text-sm text-arena-textMuted mt-1">
            Manage active franchise teams, assigned player positions, and faction color branding
          </p>
        </div>

        {isOrganizer && (
          <button
            onClick={() => setIsCreateOpen(true)}
            className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2.5 px-6 rounded-[14px] transition-all shrink-0 uppercase tracking-wider text-xs flex items-center gap-2 shadow-[0_0_18px_rgba(0,184,252,0.35)]"
          >
            <Plus size={16} />
            Register New Team
          </button>
        )}
      </div>

      {/* Controls & Search Bar */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 mb-8">
        <div className="relative w-full sm:w-80">
          <Search size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-arena-textMuted" />
          <input
            type="text"
            placeholder="Search teams by name or ID..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full bg-[var(--panel-2)] border border-arena-border hover:border-arena-cyan focus:border-arena-cyan rounded-[14px] pl-10 pr-4 py-2.5 text-sm text-white placeholder:text-arena-textMuted outline-none transition-colors"
          />
        </div>

        <div className="text-xs font-mono text-arena-textMuted">
          Showing <span className="text-white font-bold">{filteredTeams.length}</span> of {teams.length} Teams
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-950/60 border border-red-500/40 rounded-[14px] text-red-200 text-sm">
          {error}
        </div>
      )}

      {/* Teams Grid */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="bg-arena-surface border border-arena-border rounded-[14px] p-6 animate-pulse h-48" />
          ))}
        </div>
      ) : filteredTeams.length === 0 ? (
        <div className="bg-arena-surface border border-arena-border rounded-[14px] p-12 text-center shadow-lg">
          <Shield size={40} className="mx-auto text-arena-textMuted mb-3 opacity-40" />
          <h3 className="text-lg font-bold text-white mb-1">No Teams Found</h3>
          <p className="text-sm text-arena-textMuted max-w-md mx-auto mb-4">
            {searchQuery ? 'No teams match your search criteria. Try a different keyword.' : 'No esports teams registered yet.'}
          </p>
          {isOrganizer && !searchQuery && (
            <button
              onClick={() => setIsCreateOpen(true)}
              className="bg-arena-cyan text-black font-bold py-2 px-5 rounded-[14px] text-xs uppercase tracking-wider"
            >
              Register First Team
            </button>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredTeams.map((team) => (
            <div
              key={team.teamId}
              className="bg-arena-surface border border-arena-border rounded-[14px] p-6 flex flex-col justify-between hover:border-arena-cyan/40 transition-all shadow-md group relative overflow-hidden"
            >
              {/* Top Faction Accent Line */}
              <div 
                className="absolute top-0 left-0 right-0 h-1 transition-opacity"
                style={{ backgroundColor: team.colorHex }}
              />

              <div>
                {/* Team Badge & Title */}
                <div className="flex items-center gap-4 mb-4">
                  <TeamBadgeBox team={team} isOrganizer={isOrganizer} onLogoUpdated={fetchTeams} />

                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <h3 className="text-lg font-bold text-white truncate group-hover:text-arena-cyan transition-colors">
                        {team.name}
                      </h3>
                    </div>
                    <div className="flex items-center gap-2">
                      <span 
                        className="px-2.5 py-0.5 text-[10px] font-mono font-bold uppercase rounded-[14px]"
                        style={{ color: team.colorHex, backgroundColor: `${team.colorHex}20` }}
                      >
                        ID #{team.teamId}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Team Roster Summary */}
                <div className="bg-[var(--panel-2)] rounded-[14px] p-3.5 mb-5">
                  <div className="flex items-center justify-between text-xs text-arena-textMuted font-mono mb-2">
                    <span className="flex items-center gap-1.5 font-bold uppercase text-[10px] tracking-wider text-white">
                      <Users size={12} className="text-arena-cyan" /> Roster Members
                    </span>
                    <span>{team.players?.length || team.roster?.length || 0} Players</span>
                  </div>

                  <div className="flex items-center gap-1.5 flex-wrap">
                    {(team.players || team.roster || []).slice(0, 4).map((p) => (
                      <span
                        key={p.playerId}
                        className="px-2.5 py-1 bg-black/50 rounded-[14px] text-[11px] font-semibold text-arena-text truncate max-w-[110px]"
                      >
                        {p.username}
                      </span>
                    ))}
                    {(team.players?.length || team.roster?.length || 0) > 4 && (
                      <span className="text-[10px] font-mono text-arena-cyan font-bold px-1.5 py-0.5">
                        +{(team.players?.length || team.roster?.length || 0) - 4} more
                      </span>
                    )}
                    {(team.players?.length || team.roster?.length || 0) === 0 && (
                      <span className="text-xs text-arena-textMuted italic">No roster members assigned</span>
                    )}
                  </div>
                </div>
              </div>

              {/* Action Button */}
              <button
                onClick={() => setSelectedTeamId(team.teamId)}
                className="w-full bg-[var(--panel-2)] hover:bg-zinc-800 border border-arena-border hover:border-arena-cyan text-white font-bold py-2.5 px-4 rounded-[14px] transition-all text-xs uppercase tracking-wider flex items-center justify-center gap-2 group/btn"
              >
                <span>{isOrganizer ? 'Manage Team & Roster' : 'View Team Roster'}</span>
                <ChevronRight size={14} className="group-hover/btn:translate-x-1 transition-transform text-arena-cyan" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Create Team Modal */}
      {isCreateOpen && (
        <CreateTeamModal
          onClose={() => setIsCreateOpen(false)}
          onSuccess={() => {
            setIsCreateOpen(false);
            fetchTeams();
          }}
        />
      )}

      {/* Team Roster Management Drawer / Modal */}
      {selectedTeamId !== null && (
        <TeamRosterModal
          teamId={selectedTeamId}
          onClose={() => setSelectedTeamId(null)}
          onUpdate={fetchTeams}
        />
      )}
    </div>
  );
};

export default TeamsView;
