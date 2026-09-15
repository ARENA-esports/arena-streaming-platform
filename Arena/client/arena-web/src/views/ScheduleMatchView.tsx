import { FC, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import { matchService } from '../api/matchService';
import { useTheme } from '../context/ThemeContext';
import Button from '../components/common/Button';

export const ScheduleMatchView: FC = () => {
  const navigate = useNavigate();
  const { actualTheme } = useTheme();
  const [serverError, setServerError] = useState<string | null>(null);

  const formik = useFormik({
    initialValues: {
      teamAId: '',
      teamBId: '',
      scheduledStartTime: '',
    },
    validationSchema: Yup.object({
      teamAId: Yup.number().required('Team A ID is required').positive().integer(),
      teamBId: Yup.number()
        .required('Team B ID is required')
        .positive()
        .integer()
        .notOneOf([Yup.ref('teamAId')], 'Teams cannot be the same'),
      scheduledStartTime: Yup.date()
        .required('Start time is required')
        .min(new Date(), 'Scheduled time must be in the future'),
    }),
    onSubmit: async (
      values: { teamAId: string; teamBId: string; scheduledStartTime: string },
      { setSubmitting }: { setSubmitting: (isSubmitting: boolean) => void }
    ) => {
      setServerError(null);
      try {
        const response = await matchService.createMatch({
          teamAId: parseInt(values.teamAId),
          teamBId: parseInt(values.teamBId),
          scheduledTime: new Date(values.scheduledStartTime).toISOString()
        });
        navigate(`/matches/${response.matchId}`);
      } catch (err: any) {
        setServerError(err.response?.data?.message || 'Failed to schedule match. Check validation rules.');
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <div className="max-w-xl mx-auto px-4 py-12">
      <div className="bg-[var(--panel)] border border-[var(--line)] rounded-lg p-8 shadow-xl">
        <h1 className="text-3xl font-bold text-[var(--text)] mb-8">
          Schedule Match
        </h1>

        <form onSubmit={formik.handleSubmit} className="space-y-6">
          {serverError && (
            <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-sm text-sm text-center">
              {serverError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-6">
            <div>
              <label htmlFor="teamAId" className="text-xs font-mono text-[var(--muted)] mb-1 block uppercase">Team A ID</label>
              <input
                id="teamAId"
                type="number"
                className="bg-[var(--panel-2)] border border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] rounded px-3 py-2 text-sm w-full outline-none transition-colors"
                {...formik.getFieldProps('teamAId')}
              />
              {formik.touched.teamAId && formik.errors.teamAId && (
                <div className="text-arena-crimson text-xs mt-1">{formik.errors.teamAId}</div>
              )}
            </div>
            <div>
              <label htmlFor="teamBId" className="text-xs font-mono text-[var(--muted)] mb-1 block uppercase">Team B ID</label>
              <input
                id="teamBId"
                type="number"
                className="bg-[var(--panel-2)] border border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] rounded px-3 py-2 text-sm w-full outline-none transition-colors"
                {...formik.getFieldProps('teamBId')}
              />
              {formik.touched.teamBId && formik.errors.teamBId && (
                <div className="text-arena-crimson text-xs mt-1">{formik.errors.teamBId}</div>
              )}
            </div>
          </div>

          <div>
            <label htmlFor="scheduledStartTime" className="text-xs font-mono text-[var(--muted)] mb-1 block uppercase">Scheduled Start Time (Local)</label>
            <input
              id="scheduledStartTime"
              type="datetime-local"
              style={{ colorScheme: actualTheme }}
              className="bg-[var(--panel-2)] border border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] rounded px-3 py-2 text-sm w-full outline-none transition-colors"
              {...formik.getFieldProps('scheduledStartTime')}
            />
            {formik.touched.scheduledStartTime && formik.errors.scheduledStartTime && (
              <div className="text-arena-crimson text-xs mt-1">{formik.errors.scheduledStartTime as string}</div>
            )}
          </div>

          <div className="pt-4">
            <Button type="submit" className="w-full" isLoading={formik.isSubmitting}>
              Create Match
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ScheduleMatchView;
