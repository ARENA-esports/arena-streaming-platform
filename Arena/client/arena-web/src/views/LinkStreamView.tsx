import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import { matchService } from '../api/matchService';
import Input from '../components/common/Input';
import Button from '../components/common/Button';
import { Info } from 'lucide-react';

export const LinkStreamView: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const formik = useFormik({
    initialValues: {
      channelName: '',
    },
    validationSchema: Yup.object({
      channelName: Yup.string().required('Twitch channel name is required'),
    }),
    onSubmit: async (values, { setSubmitting }) => {
      setServerError(null);
      try {
        if (!matchId) throw new Error('Match ID is missing');
        
        await matchService.linkStream(parseInt(matchId), {
          channelName: values.channelName,
          embedParentDomain: window.location.hostname
        });
        
        navigate(`/matches/${matchId}`);
      } catch (err: any) {
        if (err.response?.status === 409) {
          setServerError('This match already has an active stream linked to it.');
        } else {
          setServerError(err.response?.data?.message || 'Failed to link stream. Please try again.');
        }
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <div className="max-w-2xl mx-auto px-4 py-12">
      <div className="bg-arena-surface border border-arena-border rounded-sm p-8 shadow-[0_0_50px_rgba(0,184,252,0.05)]">
        <h1 className="text-3xl font-display font-black text-white tracking-widest uppercase mb-8">
          Link Broadcast
        </h1>

        <div className="mb-8 p-4 bg-arena-cyan/10 border border-arena-cyan rounded-sm flex items-start">
          <Info className="text-arena-cyan mr-3 flex-shrink-0 mt-0.5" size={20} />
          <p className="text-sm text-arena-cyan font-sans leading-relaxed">
            Your stream will automatically transition to 'LIVE' for viewers the moment you begin streaming via OBS. There is no manual "Go Live" button here.
          </p>
        </div>

        <form onSubmit={formik.handleSubmit} className="space-y-6">
          {serverError && (
            <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-sm text-sm text-center">
              {serverError}
            </div>
          )}

          <Input
            label="Twitch Channel Name"
            id="channelName"
            placeholder="e.g. esl_csgo"
            {...formik.getFieldProps('channelName')}
            error={formik.touched.channelName ? formik.errors.channelName : undefined}
          />

          <div className="pt-4">
            <Button type="submit" className="w-full" isLoading={formik.isSubmitting}>
              Link Twitch Channel
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default LinkStreamView;
