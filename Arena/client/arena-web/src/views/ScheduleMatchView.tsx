import { FC, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import { matchService } from '../api/matchService';
import { teamService, Team } from '../api/teamService';
import Button from '../components/common/Button';
import { ArenaDatePicker } from '../components/common/ArenaDatePicker';
import { ScheduleHeader } from '../components/schedule/ScheduleHeader';
import { Check, Swords, Loader2, RefreshCw, AlertCircle } from 'lucide-react';

const getContrastColor = (hex: string): string => {
  if (!hex) return '#FFFFFF';
  const cleanHex = hex.replace('#', '');
  if (cleanHex.length !== 6) return '#FFFFFF';
  const r = parseInt(cleanHex.substring(0, 2), 16);
  const g = parseInt(cleanHex.substring(2, 4), 16);
  const b = parseInt(cleanHex.substring(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.55 ? '#000000' : '#FFFFFF';
};

const hexToRgba = (hex: string, alpha: number): string => {
  if (!hex) return `rgba(255, 255, 255, ${alpha})`;
  const cleanHex = hex.replace('#', '');
  if (cleanHex.length !== 6) return `rgba(255, 255, 255, ${alpha})`;
  const r = parseInt(cleanHex.substring(0, 2), 16);
  const g = parseInt(cleanHex.substring(2, 4), 16);
  const b = parseInt(cleanHex.substring(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

export const ScheduleMatchView: FC = () => {
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);
  const [teams, setTeams] = useState<Team[]>([]);
  const [loadingTeams, setLoadingTeams] = useState<boolean>(true);
  const [teamsError, setTeamsError] = useState<string | null>(null);

  const fetchTeams = async () => {
    setLoadingTeams(true);
    setTeamsError(null);
    try {
      const data = await teamService.getAllTeams();
      setTeams(data);
    } catch (err: any) {
      setTeamsError('Unable to load available teams. Please retry.');
    } finally {
      setLoadingTeams(false);
    }
  };

  useEffect(() => {
    fetchTeams();
  }, []);

  const formik = useFormik({
    initialValues: {
      teamAId: '',
      teamBId: '',
      scheduledStartTime: new Date(),
    },
    validationSchema: Yup.object({
      teamAId: Yup.number().required('Please select Team A').positive().integer(),
      teamBId: Yup.number()
        .required('Please select Team B')
        .positive()
        .integer()
        .notOneOf([Yup.ref('teamAId')], 'Team B cannot be the same as Team A'),
      scheduledStartTime: Yup.date()
        .required('Start time is required')
        .min(new Date(), 'Scheduled time must be in the future'),
    }),
    onSubmit: async (
      values: { teamAId: string; teamBId: string; scheduledStartTime: Date },
      { setSubmitting }: { setSubmitting: (isSubmitting: boolean) => void }
    ) => {
      setServerError(null);
      try {
        const response = await matchService.createMatch({
          teamAId: parseInt(values.teamAId),
          teamBId: parseInt(values.teamBId),
          scheduledTime: values.scheduledStartTime.toISOString(),
        });
        navigate(`/matches/${response.matchId}`);
      } catch (err: any) {
        let errorMsg = 'Failed to schedule match. Check validation rules.';
        if (err.response?.status === 400 && err.response.data?.message) {
          errorMsg = err.response.data.message;
        }
        setServerError(errorMsg);
      } finally {
        setSubmitting(false);
      }
    },
  });

  const selectedTeamA = teams.find((t) => t.teamId.toString() === formik.values.teamAId);
  const selectedTeamB = teams.find((t) => t.teamId.toString() === formik.values.teamBId);

  const renderTeamCards = (slot: 'A' | 'B') => {
    const selectedId = slot === 'A' ? formik.values.teamAId : formik.values.teamBId;
    const otherSelectedId = slot === 'A' ? formik.values.teamBId : formik.values.teamAId;
    const fieldName = slot === 'A' ? 'teamAId' : 'teamBId';

    if (loadingTeams) {
      return (
        <div className="flex items-center justify-center p-8 bg-[var(--panel-2)] border border-[var(--line)] rounded-xl">
          <Loader2 size={24} className="text-[#00B8FC] animate-spin mr-3" />
          <span className="text-sm text-[var(--muted)] font-mono">Loading active teams...</span>
        </div>
      );
    }

    if (teamsError) {
      return (
        <div className="p-4 bg-arena-crimson/10 border border-arena-crimson text-arena-crimson rounded-xl flex items-center justify-between text-xs">
          <div className="flex items-center gap-2">
            <AlertCircle size={16} />
            <span>{teamsError}</span>
          </div>
          <button
            type="button"
            onClick={fetchTeams}
            className="flex items-center gap-1 font-bold underline cursor-pointer"
          >
            <RefreshCw size={12} /> Retry
          </button>
        </div>
      );
    }

    if (teams.length === 0) {
      return (
        <div className="p-6 text-center bg-[var(--panel-2)] border border-[var(--line)] rounded-xl text-sm text-[var(--muted)] font-mono">
          No teams found in the database. Please create teams first.
        </div>
      );
    }

    return (
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        {teams.map((team) => {
          const isSelected = selectedId === team.teamId.toString();
          const isAlreadyInOtherSlot = otherSelectedId === team.teamId.toString();
          const contrastColor = getContrastColor(team.colorHex);

          return (
            <button
              key={`${slot}-${team.teamId}`}
              type="button"
              id={`team-selector-${slot.toLowerCase()}-${team.teamId}`}
              disabled={isAlreadyInOtherSlot}
              onClick={() => {
                if (!isAlreadyInOtherSlot) {
                  formik.setFieldValue(fieldName, team.teamId.toString());
                  formik.setFieldTouched(fieldName, true);
                }
              }}
              className={`group relative flex items-center justify-between p-3 rounded-lg border text-left transition-all duration-200 overflow-hidden ${
                isAlreadyInOtherSlot
                  ? 'opacity-35 cursor-not-allowed border-zinc-800 bg-zinc-900/40'
                  : isSelected
                  ? 'shadow-lg scale-[1.01] cursor-pointer'
                  : 'bg-[var(--panel-2)] hover:scale-[1.01] cursor-pointer'
              }`}
              style={{
                borderColor: isSelected ? team.colorHex : hexToRgba(team.colorHex, 0.45),
                backgroundColor: isSelected ? team.colorHex : undefined,
                boxShadow: isSelected ? `0 0 16px ${hexToRgba(team.colorHex, 0.45)}` : undefined,
              }}
            >
              {/* Colored accent line on the left side of the box */}
              <div
                className="absolute left-0 top-0 bottom-0 w-1.5 transition-colors"
                style={{
                  backgroundColor: isSelected
                    ? contrastColor === '#000000'
                      ? 'rgba(0, 0, 0, 0.4)'
                      : 'rgba(255, 255, 255, 0.7)'
                    : team.colorHex,
                }}
              />

              <div className="flex items-center gap-2.5 pl-1.5 min-w-0">
                {/* Team initials badge */}
                <div
                  className="w-7 h-7 rounded-md flex items-center justify-center font-black text-[11px] font-mono shrink-0 shadow-sm"
                  style={{
                    backgroundColor: isSelected
                      ? contrastColor === '#000000'
                        ? 'rgba(0, 0, 0, 0.15)'
                        : 'rgba(255, 255, 255, 0.2)'
                      : hexToRgba(team.colorHex, 0.2),
                    color: isSelected ? contrastColor : team.colorHex,
                    border: `1px solid ${isSelected ? 'transparent' : hexToRgba(team.colorHex, 0.35)}`,
                  }}
                >
                  {team.name.slice(0, 2).toUpperCase()}
                </div>

                <div className="min-w-0">
                  <div
                    className="font-bold text-sm tracking-wide truncate transition-colors"
                    style={{ color: isSelected ? contrastColor : 'var(--text)' }}
                  >
                    {team.name}
                  </div>
                  <div
                    className="text-[10px] font-mono font-semibold transition-colors"
                    style={{
                      color: isSelected
                        ? contrastColor === '#000000'
                          ? 'rgba(0, 0, 0, 0.7)'
                          : 'rgba(255, 255, 255, 0.75)'
                        : 'var(--muted)',
                    }}
                  >
                    ID #{team.teamId}
                  </div>
                </div>
              </div>

              {/* Status indicator on the right */}
              <div className="shrink-0 ml-2">
                {isSelected ? (
                  <div
                    className="w-5 h-5 rounded-full flex items-center justify-center shadow-sm"
                    style={{
                      backgroundColor: contrastColor === '#000000' ? '#000000' : '#FFFFFF',
                      color: contrastColor === '#000000' ? team.colorHex : '#000000',
                    }}
                  >
                    <Check size={12} strokeWidth={3.5} />
                  </div>
                ) : isAlreadyInOtherSlot ? (
                  <span className="text-[10px] font-mono text-zinc-500 bg-zinc-800/80 px-1.5 py-0.5 rounded border border-zinc-700/50">
                    In Team {slot === 'A' ? 'B' : 'A'}
                  </span>
                ) : null}
              </div>
            </button>
          );
        })}
      </div>
    );
  };

  return (
    <div className="max-w-5xl mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-[var(--text)] mb-8">Schedule</h1>

      <ScheduleHeader />

      <div className="bg-[var(--panel)] border border-[var(--line)] rounded-xl p-6 sm:p-8 shadow-2xl mt-8 max-w-3xl mx-auto">
        <div className="border-b border-[var(--line)] pb-4 mb-6">
          <h3 className="text-xl font-bold text-[var(--text)] tracking-tight">Add New Event</h3>
          <p className="text-xs text-[var(--muted)] font-mono mt-1">
            Choose two competing teams from the available list and schedule a kickoff time.
          </p>
        </div>

        <form onSubmit={formik.handleSubmit} className="space-y-6">
          {serverError && (
            <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-lg text-sm text-center">
              {serverError}
            </div>
          )}

          {/* Hidden Inputs to preserve form structure & test compatibility */}
          <input type="hidden" id="teamAId" name="teamAId" value={formik.values.teamAId} />
          <input type="hidden" id="teamBId" name="teamBId" value={formik.values.teamBId} />

          {/* Matchup Preview Banner */}
          {(selectedTeamA || selectedTeamB) && (
            <div className="p-4 rounded-xl bg-[var(--panel-2)] border border-[var(--line)] flex items-center justify-between shadow-inner">
              <div className="flex items-center gap-2.5 flex-1 min-w-0">
                {selectedTeamA ? (
                  <>
                    <div
                      className="w-3.5 h-3.5 rounded-full shrink-0 shadow-sm"
                      style={{ backgroundColor: selectedTeamA.colorHex }}
                    />
                    <div className="truncate">
                      <span className="text-xs font-mono text-[var(--muted)] block">TEAM A</span>
                      <span className="font-bold text-white text-sm sm:text-base">{selectedTeamA.name}</span>
                    </div>
                  </>
                ) : (
                  <span className="text-xs text-[var(--muted)] font-mono italic">Select Team A</span>
                )}
              </div>

              <div className="mx-3 px-3 py-1 rounded-full bg-zinc-900 border border-zinc-700/80 flex items-center gap-1.5 shrink-0 shadow-sm">
                <Swords size={13} className="text-[#00B8FC]" />
                <span className="text-xs font-mono font-bold text-[#00B8FC]">VS</span>
              </div>

              <div className="flex items-center justify-end gap-2.5 flex-1 min-w-0 text-right">
                {selectedTeamB ? (
                  <>
                    <div className="truncate">
                      <span className="text-xs font-mono text-[var(--muted)] block">TEAM B</span>
                      <span className="font-bold text-white text-sm sm:text-base">{selectedTeamB.name}</span>
                    </div>
                    <div
                      className="w-3.5 h-3.5 rounded-full shrink-0 shadow-sm"
                      style={{ backgroundColor: selectedTeamB.colorHex }}
                    />
                  </>
                ) : (
                  <span className="text-xs text-[var(--muted)] font-mono italic">Select Team B</span>
                )}
              </div>
            </div>
          )}

          {/* Team A Selection Section */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-xs font-mono text-[var(--muted)] uppercase font-bold tracking-wider">
                Team A <span className="text-zinc-500 font-normal">(Home)</span>
              </label>
              {selectedTeamA && (
                <span
                  className="text-[11px] font-bold font-mono px-2 py-0.5 rounded border"
                  style={{
                    borderColor: selectedTeamA.colorHex,
                    color: selectedTeamA.colorHex,
                    backgroundColor: `${selectedTeamA.colorHex}15`,
                  }}
                >
                  {selectedTeamA.name}
                </span>
              )}
            </div>

            {renderTeamCards('A')}

            {formik.touched.teamAId && formik.errors.teamAId && (
              <div className="text-arena-crimson text-xs mt-1 font-medium">{formik.errors.teamAId}</div>
            )}
          </div>

          {/* Team B Selection Section */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-xs font-mono text-[var(--muted)] uppercase font-bold tracking-wider">
                Team B <span className="text-zinc-500 font-normal">(Away)</span>
              </label>
              {selectedTeamB && (
                <span
                  className="text-[11px] font-bold font-mono px-2 py-0.5 rounded border"
                  style={{
                    borderColor: selectedTeamB.colorHex,
                    color: selectedTeamB.colorHex,
                    backgroundColor: `${selectedTeamB.colorHex}15`,
                  }}
                >
                  {selectedTeamB.name}
                </span>
              )}
            </div>

            {renderTeamCards('B')}

            {formik.touched.teamBId && formik.errors.teamBId && (
              <div className="text-arena-crimson text-xs mt-1 font-medium">{formik.errors.teamBId}</div>
            )}
          </div>

          {/* Match Kickoff Time Picker */}
          <div>
            <ArenaDatePicker
              value={formik.values.scheduledStartTime}
              onChange={(date) => formik.setFieldValue('scheduledStartTime', date)}
              label="SCHEDULED START TIME (LOCAL)"
            />
            {formik.touched.scheduledStartTime && formik.errors.scheduledStartTime && (
              <div className="text-arena-crimson text-xs mt-1 font-medium">
                {formik.errors.scheduledStartTime as string}
              </div>
            )}
          </div>

          {/* Submit Action */}
          <div className="pt-2">
            <Button
              type="submit"
              className="w-full bg-[#00B8FC] hover:bg-[#0096D6] text-black font-bold border-none py-3 shadow-lg hover:shadow-[#00B8FC]/20"
              isLoading={formik.isSubmitting}
              disabled={!formik.values.teamAId || !formik.values.teamBId}
            >
              Create Event
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ScheduleMatchView;
