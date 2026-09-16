import React, { useState, useEffect } from 'react';
import { teamService, Team, formatLogoUrl } from '../../api/teamService';
import { useAuth } from '../../context/AuthContext';
import { X, UserPlus, Trash2, Upload, Crown, Check, Edit2 } from 'lucide-react';

interface TeamRosterModalProps {
  teamId: number;
  onClose: () => void;
  onUpdate: () => void;
}

const PLAYER_ROLES = [
  'Captain',
  'In-Game Leader (IGL)',
  'Entry Fragger',
  'Sniper / AWPer',
  'Support',
  'Flex',
  'Coach',
  'Sub'
];

export const TeamRosterModal: React.FC<TeamRosterModalProps> = ({ teamId, onClose, onUpdate }) => {
  const { user } = useAuth();
  const isOrganizer = user?.role === 'Organizer';

  const [team, setTeam] = useState<Team | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Add player form
  const [newPlayerName, setNewPlayerName] = useState('');
  const [newPlayerRole, setNewPlayerRole] = useState(PLAYER_ROLES[2]);
  const [isAddingPlayer, setIsAddingPlayer] = useState(false);

  // Edit team form
  const [isEditingTeam, setIsEditingTeam] = useState(false);
  const [editName, setEditName] = useState('');
  const [editColor, setEditColor] = useState('');
  const [editLogoUrl, setEditLogoUrl] = useState('');
  const [isUpdatingTeam, setIsUpdatingTeam] = useState(false);

  // Logo upload & error state
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);
  const [logoImgError, setLogoImgError] = useState(false);

  const fetchTeamDetails = async () => {
    setLoading(true);
    try {
      const data = await teamService.getTeamById(teamId);
      setTeam(data);
      setEditName(data.name);
      setEditColor(data.colorHex);
      setEditLogoUrl(data.logoUrl || '');
      setLogoImgError(false);
      setError('');
    } catch (err: any) {
      setError('Failed to load team roster details.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTeamDetails();
  }, [teamId]);

  const handleAddPlayer = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newPlayerName.trim()) return;

    setIsAddingPlayer(true);
    setError('');
    setSuccessMsg('');

    try {
      await teamService.addPlayer(teamId, {
        username: newPlayerName.trim(),
        role: newPlayerRole
      });
      setNewPlayerName('');
      setSuccessMsg('Player added to roster successfully!');
      fetchTeamDetails();
      onUpdate();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to add player to roster.');
    } finally {
      setIsAddingPlayer(false);
    }
  };

  const handleRemovePlayer = async (playerId: number, playerName: string) => {
    if (!window.confirm(`Are you sure you want to remove ${playerName} from the roster?`)) return;

    setError('');
    setSuccessMsg('');
    try {
      await teamService.removePlayer(teamId, playerId);
      setSuccessMsg('Player removed from roster.');
      fetchTeamDetails();
      onUpdate();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to remove player.');
    }
  };

  const handleSaveTeamEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsUpdatingTeam(true);
    setError('');
    setSuccessMsg('');

    try {
      await teamService.updateTeam(teamId, {
        teamName: editName.trim(),
        colorHex: editColor.trim(),
        logoUrl: editLogoUrl.trim() || undefined
      });
      setIsEditingTeam(false);
      setSuccessMsg('Team details and logo saved successfully!');
      fetchTeamDetails();
      onUpdate();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update team details.');
    } finally {
      setIsUpdatingTeam(false);
    }
  };

  const handleLogoChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploadingLogo(true);
    setError('');
    setSuccessMsg('');

    try {
      const res = await teamService.uploadLogo(teamId, file);
      if (res.logoUrl) setEditLogoUrl(res.logoUrl);
      setSuccessMsg('Team logo saved successfully!');
      fetchTeamDetails();
      onUpdate();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to upload logo.');
    } finally {
      setIsUploadingLogo(false);
    }
  };

  const players = team?.players || team?.roster || [];
  const formattedLogoUrl = formatLogoUrl(team?.logoUrl);
  const isInvalidUrl = !formattedLogoUrl || formattedLogoUrl.includes('assets.arena.gg');
  const showModalLogo = !isInvalidUrl && !logoImgError;

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50 p-4 overflow-y-auto">
      <div className="bg-arena-surface border border-arena-border rounded-[14px] max-w-2xl w-full p-6 shadow-[0_0_50px_rgba(0,184,252,0.1)] relative my-auto">
        <button 
          onClick={onClose}
          className="absolute top-4 right-4 text-arena-textMuted hover:text-white transition-colors"
        >
          <X size={20} />
        </button>

        {loading ? (
          <div className="p-12 text-center text-arena-textMuted">Loading roster...</div>
        ) : team ? (
          <>
            {/* Header / Team Overview */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 pb-6 border-b border-arena-border mb-6">
              <div className="flex items-center gap-4">
                <div 
                  className="w-16 h-16 rounded-[14px] border-2 flex items-center justify-center font-bold text-lg relative overflow-hidden group shadow-md shrink-0"
                  style={{ borderColor: team.colorHex, backgroundColor: `${team.colorHex}20` }}
                >
                  {showModalLogo ? (
                    <img 
                      src={formattedLogoUrl} 
                      alt={team.name} 
                      className="w-full h-full object-cover" 
                      onError={() => setLogoImgError(true)}
                    />
                  ) : (
                    <span style={{ color: team.colorHex }} className="font-bold">
                      {(team.name || 'TM').substring(0, 2).toUpperCase()}
                    </span>
                  )}

                  {isOrganizer && (
                    <label className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 flex items-center justify-center cursor-pointer transition-opacity" title="Upload Team Logo">
                      <Upload size={18} className="text-white" />
                      <input type="file" accept="image/*" onChange={handleLogoChange} className="hidden" disabled={isUploadingLogo} />
                    </label>
                  )}
                </div>

                <div>
                  <div className="flex items-center gap-3">
                    <h2 className="text-2xl font-bold text-white tracking-tight">{team.name}</h2>
                    <span 
                      className="px-2.5 py-0.5 text-[10px] font-mono font-bold uppercase rounded-[14px] border"
                      style={{ borderColor: `${team.colorHex}40`, color: team.colorHex, backgroundColor: `${team.colorHex}15` }}
                    >
                      {team.colorHex}
                    </span>
                  </div>
                  <p className="text-xs text-arena-textMuted mt-1">Team ID: #{team.teamId} • Roster Size: {players.length} Players</p>
                </div>
              </div>

              {isOrganizer && (
                <button
                  onClick={() => setIsEditingTeam(!isEditingTeam)}
                  className="bg-[var(--panel-2)] hover:bg-zinc-800 border border-arena-border hover:border-arena-cyan text-white text-xs font-bold py-2 px-4 rounded-[14px] transition-all flex items-center gap-2"
                >
                  <Edit2 size={14} />
                  {isEditingTeam ? 'Close Edit' : 'Edit Team'}
                </button>
              )}
            </div>

            {/* Notifications */}
            {error && <div className="mb-4 p-3 bg-red-950/60 border border-red-500/40 rounded-[14px] text-red-200 text-xs">{error}</div>}
            {successMsg && <div className="mb-4 p-3 bg-emerald-950/60 border border-emerald-500/40 rounded-[14px] text-emerald-200 text-xs flex items-center gap-2"><Check size={16} />{successMsg}</div>}

            {/* Edit Team Form */}
            {isEditingTeam && (
              <form onSubmit={handleSaveTeamEdit} className="bg-black/50 border border-arena-border rounded-[14px] p-4 mb-6 space-y-4">
                <h3 className="text-xs font-mono font-bold uppercase text-arena-cyan tracking-wider">Edit Team Details & Logo</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs text-arena-textMuted mb-1">Team Name</label>
                    <input
                      type="text"
                      value={editName}
                      onChange={(e) => setEditName(e.target.value)}
                      className="w-full bg-black border border-arena-border rounded-[14px] px-3 py-2 text-sm text-white focus:border-arena-cyan outline-none"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-arena-textMuted mb-1">Faction Color Hex</label>
                    <div className="flex gap-2">
                      <input
                        type="color"
                        value={editColor}
                        onChange={(e) => setEditColor(e.target.value)}
                        className="w-9 h-9 rounded-[14px] border border-arena-border bg-black cursor-pointer p-0.5"
                      />
                      <input
                        type="text"
                        value={editColor}
                        onChange={(e) => setEditColor(e.target.value)}
                        className="flex-1 bg-black border border-arena-border rounded-[14px] px-3 py-2 text-sm text-white font-mono focus:border-arena-cyan outline-none uppercase"
                        pattern="^#[0-9A-Fa-f]{6}$"
                        required
                      />
                    </div>
                  </div>
                </div>

                <div>
                  <label className="block text-xs text-arena-textMuted mb-1">Team Logo URL / Image File</label>
                  <div className="flex items-center gap-2">
                    <input
                      type="text"
                      value={editLogoUrl}
                      onChange={(e) => setEditLogoUrl(e.target.value)}
                      placeholder="Enter image URL or select file..."
                      className="flex-1 bg-black border border-arena-border rounded-[14px] px-3 py-2 text-sm text-white focus:border-arena-cyan outline-none"
                    />
                    <label className="bg-[var(--panel-2)] hover:bg-zinc-800 border border-arena-border hover:border-arena-cyan text-white text-xs font-bold py-2 px-3 rounded-[14px] transition-all cursor-pointer flex items-center gap-1.5 shrink-0">
                      <Upload size={14} />
                      Choose File
                      <input type="file" accept="image/*" onChange={handleLogoChange} className="hidden" disabled={isUploadingLogo} />
                    </label>
                  </div>
                </div>

                <div className="flex justify-end gap-2 pt-2">
                  <button
                    type="submit"
                    disabled={isUpdatingTeam}
                    className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold text-xs uppercase tracking-wider py-2.5 px-5 rounded-[14px] transition-all shadow-[0_0_15px_rgba(0,184,252,0.3)]"
                  >
                    {isUpdatingTeam ? 'Saving Team...' : 'Save Team Logo & Details'}
                  </button>
                </div>
              </form>
            )}

            {/* Roster Players List */}
            <div className="mb-6">
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-xs font-mono font-bold uppercase text-arena-textMuted tracking-wider">
                  Active Roster ({players.length})
                </h3>
              </div>

              {players.length === 0 ? (
                <div className="bg-black/40 border border-arena-border rounded-[14px] p-6 text-center text-arena-textMuted text-sm">
                  No active players in roster. Add players below.
                </div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-h-60 overflow-y-auto pr-1">
                  {players.map((p) => (
                    <div 
                      key={p.playerId}
                      className="bg-[var(--panel-2)] border border-arena-border rounded-[14px] p-3 flex items-center justify-between gap-3 hover:border-arena-cyan/30 transition-all"
                    >
                      <div className="flex items-center gap-3 min-w-0">
                        <div className="w-9 h-9 rounded-full bg-arena-surface border border-arena-border flex items-center justify-center text-white shrink-0 font-bold text-xs">
                          {p.username.substring(0, 2).toUpperCase()}
                        </div>
                        <div className="truncate">
                          <div className="flex items-center gap-1.5">
                            <span className="font-bold text-sm text-white truncate">{p.username}</span>
                            {p.role === 'Captain' && <Crown size={12} className="text-amber-400 shrink-0" />}
                          </div>
                          <span className="text-[11px] text-arena-cyan font-mono">{p.role || 'Player'}</span>
                        </div>
                      </div>

                      {isOrganizer && (
                        <button
                          onClick={() => handleRemovePlayer(p.playerId, p.username)}
                          className="text-arena-textMuted hover:text-red-400 p-1.5 rounded-[14px] hover:bg-red-500/10 transition-colors"
                          title="Remove from roster"
                        >
                          <Trash2 size={16} />
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Add Player Form (Organizer Only) */}
            {isOrganizer && (
              <form onSubmit={handleAddPlayer} className="border-t border-arena-border pt-4 mt-4">
                <h3 className="text-xs font-mono font-bold uppercase text-arena-textMuted tracking-wider mb-3 flex items-center gap-2">
                  <UserPlus size={14} className="text-arena-cyan" />
                  Add Player to Roster
                </h3>

                <div className="flex flex-col sm:flex-row gap-3">
                  <input
                    type="text"
                    value={newPlayerName}
                    onChange={(e) => setNewPlayerName(e.target.value)}
                    placeholder="Player IGN / Handle"
                    className="flex-1 bg-black border border-arena-border rounded-[14px] px-4 py-2 text-sm text-white focus:border-arena-cyan outline-none"
                    required
                  />

                  <select
                    value={newPlayerRole}
                    onChange={(e) => setNewPlayerRole(e.target.value)}
                    className="bg-black border border-arena-border rounded-[14px] px-4 py-2 text-sm text-white focus:border-arena-cyan outline-none"
                  >
                    {PLAYER_ROLES.map((role) => (
                      <option key={role} value={role} className="bg-zinc-900 text-white">
                        {role}
                      </option>
                    ))}
                  </select>

                  <button
                    type="submit"
                    disabled={isAddingPlayer || !newPlayerName.trim()}
                    className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2 px-5 rounded-[14px] transition-all text-xs uppercase tracking-wider shrink-0 disabled:opacity-50"
                  >
                    {isAddingPlayer ? 'Adding...' : 'Add Player'}
                  </button>
                </div>
              </form>
            )}
          </>
        ) : null}
      </div>
    </div>
  );
};
