import React, { useState } from 'react';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import AuthLayout from '../components/layout/AuthLayout';
import Input from '../components/common/Input';
import Button from '../components/common/Button';
import { authService } from '../api/authService';

export const ForgotPasswordView: React.FC = () => {
  const [statusMessage, setStatusMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

  const formik = useFormik({
    initialValues: {
      email: '',
    },
    validationSchema: Yup.object({
      email: Yup.string().email('Invalid email address').required('Email is required'),
    }),
    onSubmit: async (values: any, { setSubmitting }: any) => {
      setStatusMessage(null);
      try {
        const response = await authService.forgotPassword(values);
        setStatusMessage({ type: 'success', text: response.message });
      } catch (err: any) {
        setStatusMessage({ type: 'error', text: err.response?.data?.message || 'An error occurred. Please try again.' });
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <AuthLayout title="Recover Access" subtitle="Enter your email to receive a password reset link.">
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
          label="Email Address"
          id="email"
          type="email"
          {...formik.getFieldProps('email')}
          error={formik.touched.email ? formik.errors.email : undefined}
        />

        <Button
          type="submit"
          className="w-full"
          isLoading={formik.isSubmitting}
        >
          Send Reset Link
        </Button>
      </form>
    </AuthLayout>
  );
};

export default ForgotPasswordView;
