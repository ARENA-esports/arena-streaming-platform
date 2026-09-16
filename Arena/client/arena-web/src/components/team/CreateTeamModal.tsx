import React, { useState } from 'react';
import { teamService } from '../../api/teamService';
import { X, Swords, Upload } from 'lucide-react';

interface CreateTeamModalProps {
  onClose: () => void;
  onSuccess: () => void;
}

const PRESET_COLORS = [
  '#00B8FC', // Arena Cyan
  '#FF2B56', // Crimson
  '#10B981', // Emerald
  '#F59E0B', // Amber
  '#8B5CF6', // Purple
  '#EC4899', // Pink
  '#3B82F6', // Cobalt
  '#6366F1', // Indigo
];

export const CreateTeamModal: React.FC<CreateTeamModalProps> = ({ onClose, onSuccess }) => {
  const [teamName, setTeamName] = useState('');
  const [colorHex, setColorHex] = useState('#00B8FC');
  const [logoUrl, setLogoUrl] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        setLogoUrl(reader.result);
      }
    };
    reader.readAsDataURL(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!teamName.trim()) return;

    setIsSubmitting(true);
    setError('');

    try {
      const created = await teamService.createTeam({
        teamName: teamName.trim(),
        colorHex: colorHex.trim()
      });

      if (logoUrl.trim() && created?.teamId) {
        await teamService.updateTeam(created.teamId, { logoUrl: logoUrl.trim() });
      }

      onSuccess();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to register team.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50 p-4 overflow-y-auto">
      <div className="bg-arena-surface rounded-[14px] p-6 max-w-md w-full shadow-[0_0_50px_rgba(0,184,252,0.1)] relative">
        <button 
          onClick={onClose}
          className="absolute top-4 right-4 text-arena-textMuted hover:text-white transition-colors"
        >
          <X size={20} />
        </button>

        <div className="flex items-center gap-3 mb-6">
          <div className="w-10 h-10 rounded-[14px] bg-arena-cyan/15 flex items-center justify-center text-arena-cyan shrink-0">
            <Swords size={22} />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white tracking-tight">Register Team</h2>
            <p className="text-xs text-arena-textMuted">Create a new esports roster entry with faction branding</p>
          </div>
        </div>

        {error && (
          <div className="mb-4 p-3 bg-red-950/60 rounded-[14px] text-red-200 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1">
              Team Name
            </label>
            <input 
              type="text" 
              value={teamName}
              onChange={(e) => setTeamName(e.target.value)}
              placeholder="e.g. Crimson Vipers"
              className="w-full bg-black rounded-[14px] px-4 py-2.5 focus:ring-1 focus:ring-arena-cyan outline-none text-white text-sm transition-colors"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1">
              Team Logo (Optional)
            </label>
            <div className="flex items-center gap-2">
              <input 
                type="text" 
                value={logoUrl}
                onChange={(e) => setLogoUrl(e.target.value)}
                placeholder="Enter logo URL or choose file..."
                className="flex-1 bg-black rounded-[14px] px-4 py-2 text-sm text-white focus:ring-1 focus:ring-arena-cyan outline-none"
              />
              <label className="bg-[var(--panel-2)] hover:bg-zinc-800 text-white font-bold py-2 px-3 rounded-[14px] transition-all cursor-pointer flex items-center gap-1.5 shrink-0 text-xs">
                <Upload size={14} />
                Upload
                <input type="file" accept="image/*" onChange={handleFileUpload} className="hidden" />
              </label>
            </div>
          </div>

          <div>
            <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1.5">
              Team Faction Color
            </label>
            <div className="flex items-center gap-3 mb-3">
              <input 
                type="color" 
                value={colorHex}
                onChange={(e) => setColorHex(e.target.value)}
                className="w-10 h-10 rounded-[14px] bg-black cursor-pointer p-1"
              />
              <input 
                type="text" 
                value={colorHex}
                onChange={(e) => setColorHex(e.target.value)}
                placeholder="#00B8FC"
                className="flex-1 bg-black rounded-[14px] px-4 py-2 font-mono text-sm uppercase text-white focus:ring-1 focus:ring-arena-cyan outline-none"
                pattern="^#[0-9A-Fa-f]{6}$"
                required
              />
            </div>
            
            {/* Color Presets */}
            <div className="flex items-center gap-2 flex-wrap">
              {PRESET_COLORS.map((color) => (
                <button
                  key={color}
                  type="button"
                  onClick={() => setColorHex(color)}
                  className={`w-7 h-7 rounded-full transition-transform ${colorHex.toUpperCase() === color.toUpperCase() ? 'scale-110 ring-2 ring-arena-cyan' : 'hover:scale-105'}`}
                  style={{ backgroundColor: color }}
                  title={color}
                />
              ))}
            </div>
          </div>

          {/* Live Badge Preview */}
          <div className="pt-2">
            <label className="block text-[11px] font-mono text-arena-textMuted uppercase mb-1">
              Branding Badge Preview
            </label>
            <div className="bg-black/50 rounded-[14px] p-3 flex items-center gap-3">
              <div 
                className="w-10 h-10 rounded-[14px] flex items-center justify-center font-bold text-xs overflow-hidden shrink-0"
                style={{ color: colorHex, backgroundColor: `${colorHex}20` }}
              >
                {logoUrl ? (
                  <img src={logoUrl} alt="Preview" className="w-full h-full object-cover" />
                ) : (
                  teamName ? teamName.substring(0, 2).toUpperCase() : 'TM'
                )}
              </div>
              <div>
                <div className="font-bold text-sm text-white">{teamName || 'Team Name'}</div>
              </div>
            </div>
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
              disabled={isSubmitting || !teamName.trim()}
              className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2.5 px-6 rounded-[14px] transition-all disabled:opacity-50 text-xs uppercase tracking-wider shadow-[0_0_15px_rgba(0,184,252,0.3)]"
            >
              {isSubmitting ? 'Registering...' : 'Register Team'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
