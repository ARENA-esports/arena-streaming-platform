import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import AuthLayout from '../components/layout/AuthLayout';
import Input from '../components/common/Input';
import Button from '../components/common/Button';
import { authService } from '../api/authService';

export const ResetPasswordView: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const queryParams = new URLSearchParams(location.search);
  const token = queryParams.get('token') || '';

  const [statusMessage, setStatusMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

  const formik = useFormik({
    initialValues: {
      password: '',
      confirmPassword: '',
    },
    validationSchema: Yup.object({
      password: Yup.string().required('Password is required').min(8, 'Password must be at least 8 characters'),
      confirmPassword: Yup.string()
        .oneOf([Yup.ref('password')], 'Passwords must match')
        .required('Confirm Password is required'),
    }),
    onSubmit: async (values: any, { setSubmitting }: any) => {
      setStatusMessage(null);
      if (!token) {
        setStatusMessage({ type: 'error', text: 'Reset token is missing from the URL.' });
        setSubmitting(false);
        return;
      }

      try {
        await authService.resetPassword({
          token,
          newPassword: values.password
        });
        setStatusMessage({ type: 'success', text: 'Password has been successfully reset. Redirecting...' });
        setTimeout(() => navigate('/login'), 2000);
      } catch (err: any) {
        setStatusMessage({ type: 'error', text: err.response?.data?.message || 'Failed to reset password. The token may be expired or invalid.' });
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <AuthLayout title="Reset Password" subtitle="Enter your new secure password.">
      <form onSubmit={formik.handleSubmit} className="space-y-6">
        {statusMessage && (
          <div className={`p-3 rounded-sm text-sm text-center border ${
            statusMessage.type === 'success' 
              ? 'bg-green-900/20 border-green-500 text-green-400' 
              : 'bg-arena-crimson/10 border-arena-crimson text-arena-crimson'
          }`}>
            {statusMessage.text}
          </div>
        )}

        <Input
          label="New Password"
          id="password"
          type="password"
          {...formik.getFieldProps('password')}
          error={formik.touched.password ? formik.errors.password : undefined}
        />

        <Input
          label="Confirm New Password"
          id="confirmPassword"
          type="password"
          {...formik.getFieldProps('confirmPassword')}
          error={formik.touched.confirmPassword ? formik.errors.confirmPassword : undefined}
        />

        <Button
          type="submit"
          className="w-full"
          isLoading={formik.isSubmitting}
          disabled={!token || statusMessage?.type === 'success'}
        >
          Update Password
        </Button>
      </form>
    </AuthLayout>
  );
};

export default ResetPasswordView;
