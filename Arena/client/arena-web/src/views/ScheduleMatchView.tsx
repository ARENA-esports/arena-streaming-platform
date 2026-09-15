import { FC, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import { matchService } from '../api/matchService';
import Button from '../components/common/Button';
import { ArenaDatePicker } from '../components/common/ArenaDatePicker';
import { ScheduleHeader } from '../components/schedule/ScheduleHeader';

export const ScheduleMatchView: FC = () => {
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const formik = useFormik({
    initialValues: {
      teamAId: '',
      teamBId: '',
      scheduledStartTime: new Date(),
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
      values: { teamAId: string; teamBId: string; scheduledStartTime: Date },
      { setSubmitting }: { setSubmitting: (isSubmitting: boolean) => void }
    ) => {
      setServerError(null);
      try {
        const response = await matchService.createMatch({
          teamAId: parseInt(values.teamAId),
          teamBId: parseInt(values.teamBId),
          scheduledTime: values.scheduledStartTime.toISOString()
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

  return (
    <div className="max-w-5xl mx-auto px-4 py-8">
      
      <h1 className="text-3xl font-bold text-[var(--text)] mb-8">
        Schedule
      </h1>
      
      <ScheduleHeader />

      <div className="bg-[var(--panel)] border border-[var(--line)] rounded-lg p-8 shadow-xl mt-8 max-w-2xl">
        <h3 className="text-lg font-bold text-[var(--text)] mb-6 border-b border-[var(--line)] pb-2">Add New Event</h3>
        <form onSubmit={formik.handleSubmit} className="space-y-6">
          {serverError && (
            <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-sm text-sm text-center">
              {serverError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-6">
            <div>
              <label htmlFor="teamAId" className="text-xs font-mono text-[var(--muted)] mb-1 block uppercase font-bold">Team A ID</label>
              <input
                id="teamAId"
                type="number"
                min="1"
                onKeyDown={(e) => {
                  if (['-', 'e', 'E', '+', '.'].includes(e.key)) {
                    e.preventDefault();
                  }
                }}
                className="bg-[var(--panel-2)] border border-[var(--line)] focus:border-[#00B8FC] text-[var(--text)] rounded px-3 py-2.5 text-sm w-full outline-none transition-colors"
                {...formik.getFieldProps('teamAId')}
              />
              {formik.touched.teamAId && formik.errors.teamAId && (
                <div className="text-arena-crimson text-xs mt-1">{formik.errors.teamAId}</div>
              )}
            </div>
            <div>
              <label htmlFor="teamBId" className="text-xs font-mono text-[var(--muted)] mb-1 block uppercase font-bold">Team B ID</label>
              <input
                id="teamBId"
                type="number"
                min="1"
                onKeyDown={(e) => {
                  if (['-', 'e', 'E', '+', '.'].includes(e.key)) {
                    e.preventDefault();
                  }
                }}
                className="bg-[var(--panel-2)] border border-[var(--line)] focus:border-[#00B8FC] text-[var(--text)] rounded px-3 py-2.5 text-sm w-full outline-none transition-colors"
                {...formik.getFieldProps('teamBId')}
              />
              {formik.touched.teamBId && formik.errors.teamBId && (
                <div className="text-arena-crimson text-xs mt-1">{formik.errors.teamBId}</div>
              )}
            </div>
          </div>

          <div>
            <ArenaDatePicker
              value={formik.values.scheduledStartTime}
              onChange={(date) => formik.setFieldValue('scheduledStartTime', date)}
              label="SCHEDULED START TIME (LOCAL)"
            />
            {formik.touched.scheduledStartTime && formik.errors.scheduledStartTime && (
              <div className="text-arena-crimson text-xs mt-1">{formik.errors.scheduledStartTime as string}</div>
            )}
          </div>

          <div className="pt-4">
            <Button type="submit" className="w-full bg-[#00B8FC] hover:bg-[#0096D6] text-black border-none" isLoading={formik.isSubmitting}>
              Create Event
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ScheduleMatchView;
