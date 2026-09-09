import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useFormik } from 'formik';
import * as Yup from 'yup';
import AuthLayout from '../components/layout/AuthLayout';
import Input from '../components/common/Input';
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

        <Input
          label="Username"
          id="username"
          {...formik.getFieldProps('username')}
          error={formik.touched.username ? formik.errors.username : undefined}
        />
        
        <Input
          label="Email Address"
          id="email"
          type="email"
          {...formik.getFieldProps('email')}
          error={formik.touched.email ? formik.errors.email : undefined}
        />
        
        <Input
          label="Password"
          id="password"
          type="password"
          {...formik.getFieldProps('password')}
          error={formik.touched.password ? formik.errors.password : undefined}
        />

        <Input
          label="Confirm Password"
          id="confirmPassword"
          type="password"
          {...formik.getFieldProps('confirmPassword')}
          error={formik.touched.confirmPassword ? formik.errors.confirmPassword : undefined}
        />

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
