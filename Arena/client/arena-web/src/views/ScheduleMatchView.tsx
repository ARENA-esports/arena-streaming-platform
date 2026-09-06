import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import { matchService } from '../api/matchService';
import Input from '../components/common/Input';
import Button from '../components/common/Button';

export const ScheduleMatchView: React.FC = () => {
  const navigate = useNavigate();
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
    onSubmit: async (values, { setSubmitting }) => {
      setServerError(null);
      try {
        const response = await matchService.createMatch({
          teamAId: parseInt(values.teamAId),
          teamBId: parseInt(values.teamBId),
          scheduledStartTime: new Date(values.scheduledStartTime).toISOString()
        });
        navigate(`/matches/${response.id}`);
      } catch (err: any) {
        setServerError(err.response?.data?.message || 'Failed to schedule match. Check validation rules.');
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <div className="max-w-2xl mx-auto px-4 py-12">
      <div className="bg-arena-surface border border-arena-border rounded-sm p-8 shadow-[0_0_50px_rgba(0,184,252,0.05)]">
        <h1 className="text-3xl font-display font-black text-white tracking-widest uppercase mb-8">
          Schedule Match
        </h1>

        <form onSubmit={formik.handleSubmit} className="space-y-6">
          {serverError && (
            <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-sm text-sm text-center">
              {serverError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-6">
            <Input
              label="Team A ID"
              id="teamAId"
              type="number"
              {...formik.getFieldProps('teamAId')}
              error={formik.touched.teamAId ? formik.errors.teamAId : undefined}
            />
            <Input
              label="Team B ID"
              id="teamBId"
              type="number"
              {...formik.getFieldProps('teamBId')}
              error={formik.touched.teamBId ? formik.errors.teamBId : undefined}
            />
          </div>

          <Input
            label="Scheduled Start Time (Local)"
            id="scheduledStartTime"
            type="datetime-local"
            {...formik.getFieldProps('scheduledStartTime')}
            error={formik.touched.scheduledStartTime ? (formik.errors.scheduledStartTime as string) : undefined}
          />

          <div className="pt-4">
            <Button type="submit" className="w-full" isLoading={formik.isSubmitting}>
              Create Fixture
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ScheduleMatchView;
