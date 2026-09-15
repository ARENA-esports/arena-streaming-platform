import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import AuthLayout from '../components/layout/AuthLayout';
import Button from '../components/common/Button';
import { authService } from '../api/authService';

export const SignupView: React.FC = () => {
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const formik = useFormik({
    initialValues: {
      username: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
    validationSchema: Yup.object({
      username: Yup.string().required('Username is required').min(3, 'Username must be at least 3 characters'),
      email: Yup.string().email('Invalid email address').required('Email is required'),
      password: Yup.string().required('Password is required').min(8, 'Password must be at least 8 characters'),
      confirmPassword: Yup.string()
        .oneOf([Yup.ref('password')], 'Passwords must match')
        .required('Confirm Password is required'),
    }),
    onSubmit: async (values: any, { setSubmitting }: any) => {
      setServerError(null);
      try {
        await authService.signup({
          username: values.username,
          email: values.email,
          password: values.password
        });
        navigate('/login', { state: { message: 'Registration successful! Please sign in.' } });
      } catch (err: any) {
        setServerError(err.response?.data?.message || 'Registration failed. Please try again.');
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <AuthLayout title="Initialize Identity" subtitle="Register a new viewer account.">
      <form onSubmit={formik.handleSubmit} className="space-y-6">
        {serverError && (
          <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-sm text-sm text-center">
            {serverError}
          </div>
        )}

        <div className="space-y-1">
          <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-[var(--muted)]">
            Username
          </label>
          <input
            id="username"
            type="text"
            className="w-full bg-transparent border-b border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] py-2.5 outline-none transition-colors placeholder:text-[var(--muted)]"
            {...formik.getFieldProps('username')}
          />
          {formik.touched.username && formik.errors.username && (
            <div className="text-arena-crimson text-xs mt-1">{formik.errors.username as string}</div>
          )}
        </div>
        
        <div className="space-y-1">
          <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-[var(--muted)]">
            Email Address
          </label>
          <input
            id="email"
            type="email"
            className="w-full bg-transparent border-b border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] py-2.5 outline-none transition-colors placeholder:text-[var(--muted)]"
            {...formik.getFieldProps('email')}
          />
          {formik.touched.email && formik.errors.email && (
            <div className="text-arena-crimson text-xs mt-1">{formik.errors.email as string}</div>
          )}
        </div>
        
        <div className="space-y-1">
          <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-[var(--muted)]">
            Password
          </label>
          <input
            id="password"
            type="password"
            className="w-full bg-transparent border-b border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] py-2.5 outline-none transition-colors placeholder:text-[var(--muted)]"
            {...formik.getFieldProps('password')}
          />
          {formik.touched.password && formik.errors.password && (
            <div className="text-arena-crimson text-xs mt-1">{formik.errors.password as string}</div>
          )}
        </div>

        <div className="space-y-1">
          <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-[var(--muted)]">
            Confirm Password
          </label>
          <input
            id="confirmPassword"
            type="password"
            className="w-full bg-transparent border-b border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] py-2.5 outline-none transition-colors placeholder:text-[var(--muted)]"
            {...formik.getFieldProps('confirmPassword')}
          />
          {formik.touched.confirmPassword && formik.errors.confirmPassword && (
            <div className="text-arena-crimson text-xs mt-1">{formik.errors.confirmPassword as string}</div>
          )}
        </div>

        <Button
          type="submit"
          className="w-full"
          isLoading={formik.isSubmitting}
        >
          Sign Up
        </Button>
      </form>
      
      <p className="mt-8 text-center text-sm text-arena-textMuted">
        Already have an account?{' '}
        <Link to="/login" className="font-bold text-arena-cyan hover:text-arena-cyanHover transition-colors uppercase tracking-widest text-xs">
          Sign In
        </Link>
      </p>
    </AuthLayout>
  );
};

export default SignupView;
