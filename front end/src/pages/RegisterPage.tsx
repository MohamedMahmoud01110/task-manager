import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { applyApiErrors } from '../lib/formErrors';

// Same rules as the API validators
const schema = z
  .object({
    name: z.string().trim().min(1, 'Name is required.').max(100, 'Name cannot exceed 100 characters.'),
    email: z.string().trim().min(1, 'Email is required.').max(254, 'Email is too long.').email('Email format is invalid.'),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters.')
      .max(100, 'Password cannot exceed 100 characters.')
      .regex(/[A-Za-z]/, 'Password must contain a letter.')
      .regex(/[0-9]/, 'Password must contain a digit.'),
    confirmPassword: z.string(),
  })
  .refine((v) => v.password === v.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  });
type FormValues = z.infer<typeof schema>;

export function RegisterPage() {
  const { user, register: signUp } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  if (user) return <Navigate to="/" replace />;

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await signUp(values.name, values.email, values.password);
    } catch (err) {
      // 409 (email already used) is shown under the email field. Form input is kept.
      setFormError(applyApiErrors(err, setError, ['name', 'email', 'password'], 'email'));
    }
  });

  return (
    <div className="auth-page">
      <form className="card" onSubmit={onSubmit} noValidate>
        <h1>Create account</h1>
        {formError && <div className="alert alert-error" role="alert">{formError}</div>}

        <div className="field">
          <label htmlFor="name">Name</label>
          <input id="name" autoComplete="name" {...register('name')} />
          {errors.name && <p className="error">{errors.name.message}</p>}
        </div>

        <div className="field">
          <label htmlFor="email">Email</label>
          <input id="email" type="email" autoComplete="email" {...register('email')} />
          {errors.email && <p className="error">{errors.email.message}</p>}
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input id="password" type="password" autoComplete="new-password" {...register('password')} />
          {errors.password && <p className="error">{errors.password.message}</p>}
        </div>

        <div className="field">
          <label htmlFor="confirmPassword">Confirm password</label>
          <input id="confirmPassword" type="password" autoComplete="new-password" {...register('confirmPassword')} />
          {errors.confirmPassword && <p className="error">{errors.confirmPassword.message}</p>}
        </div>

        <button className="btn btn-primary" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating…' : 'Create account'}
        </button>
        <p className="muted">Already registered? <Link to="/login">Sign in</Link></p>
      </form>
    </div>
  );
}