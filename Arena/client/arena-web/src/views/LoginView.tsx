import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import AuthLayout from '../components/layout/AuthLayout';
import Button from '../components/common/Button';
import { useAuth } from '../context/AuthContext';

export const LoginView: React.FC = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [serverError, setServerError] = useState<string | null>(null);
  
  const from = location.state?.from?.pathname || '/';

  const formik = useFormik({
    initialValues: {
      identifier: '',
      password: '',
    },
    validationSchema: Yup.object({
      identifier: Yup.string().required('Username or Email is required'),
      password: Yup.string().required('Password is required'),
    }),
    onSubmit: async (values: any, { setSubmitting }: any) => {
      setServerError(null);
      try {
        await login(values);
        navigate(from, { replace: true });
      } catch (err: any) {
        setServerError(err.response?.data?.message || 'Login failed. Please verify your credentials.');
      } finally {
        setSubmitting(false);
      }
    },
  });

  return (
    <AuthLayout title="Authenticate" subtitle="Enter your credentials to access the Arena.">
      <form onSubmit={formik.handleSubmit} className="space-y-6">
        {serverError && (
          <div className="bg-arena-crimson/10 border border-arena-crimson text-arena-crimson p-3 rounded-sm text-sm text-center">
            {serverError}
          </div>
        )}
        
        <div className="space-y-1">
          <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-[var(--muted)]">
            Username or Email
          </label>
          <input
            id="identifier"
            type="text"
            className="w-full bg-transparent border-b border-[var(--line)] focus:border-[var(--prime)] text-[var(--text)] py-2.5 outline-none transition-colors placeholder:text-[var(--muted)]"
            {...formik.getFieldProps('identifier')}
            autoComplete="username"
          />
          {formik.touched.identifier && formik.errors.identifier && (
            <div className="text-arena-crimson text-xs mt-1">{formik.errors.identifier as string}</div>
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
            autoComplete="current-password"
          />
          {formik.touched.password && formik.errors.password && (
            <div className="text-arena-crimson text-xs mt-1">{formik.errors.password as string}</div>
          )}
        </div>

        <div className="flex items-center justify-end">
          <div className="text-sm">
            <Link to="/forgot-password" className="font-bold text-arena-cyan hover:text-arena-cyanHover transition-colors uppercase tracking-widest text-xs">
              Forgot your password?
            </Link>
          </div>
        </div>

        <Button
          type="submit"
          className="w-full"
          isLoading={formik.isSubmitting}
        >
          Sign In
        </Button>
      </form>
    </AuthLayout>
  );
};

export default LoginView;
