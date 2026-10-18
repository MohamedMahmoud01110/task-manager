import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link, Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { applyApiErrors } from "../lib/formErrors";

const schema = z.object({
  email: z
    .string()
    .trim()
    .min(1, "Email is required.")
    .email("Email format is invalid."),
  password: z.string().min(1, "Password is required."),
});
type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const { user, login, sessionMessage } = useAuth();
  const location = useLocation();
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const fromLocation = (
    location.state as { from?: { pathname: string; search: string } } | null
  )?.from;
  const target = fromLocation
    ? fromLocation.pathname + fromLocation.search
    : "/";

  if (user) return <Navigate to={target} replace />;

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await login(values.email, values.password);
    } catch (err) {
      setFormError(applyApiErrors(err, setError, ["email", "password"]));
    }
  });

  return (
    <div className="auth-page">
      <form className="card" onSubmit={onSubmit} noValidate>
        <h1>Sign in</h1>

        {sessionMessage && (
          <div className="alert alert-warning" role="alert">
            {sessionMessage}
          </div>
        )}
        {formError && (
          <div className="alert alert-error" role="alert">
            {formError}
          </div>
        )}

        <div className="field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            {...register("email")}
          />
          {errors.email && <p className="error">{errors.email.message}</p>}
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            {...register("password")}
          />
          {errors.password && (
            <p className="error">{errors.password.message}</p>
          )}
        </div>

        <button
          className="btn btn-primary"
          type="submit"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Signing in…" : "Sign in"}
        </button>
        <p className="muted">
          No account? <Link to="/register">Create one</Link>
        </p>
      </form>
    </div>
  );
}
