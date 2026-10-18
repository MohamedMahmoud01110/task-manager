import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { adminUsersApi } from "../api/adminUsers";
import { applyApiErrors } from "../lib/formErrors";

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Name is required.")
    .max(100, "Name cannot exceed 100 characters."),
  email: z
    .string()
    .trim()
    .min(1, "Email is required.")
    .max(254, "Email is too long.")
    .email("Email format is invalid."),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters.")
    .max(100, "Password cannot exceed 100 characters.")
    .regex(/[A-Za-z]/, "Password must contain a letter.")
    .regex(/[0-9]/, "Password must contain a digit."),
  role: z.enum(["User", "Admin"]),
});
type FormValues = z.infer<typeof schema>;

export function AdminCreateUserPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [formError, setFormError] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: adminUsersApi.create,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["admin-users"] }),
  });

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { role: "User" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await create.mutateAsync(values);
      navigate("/admin/users");
    } catch (err) {
      setFormError(
        applyApiErrors(
          err,
          setError,
          ["name", "email", "password", "role"],
          "email",
        ),
      );
    }
  });

  return (
    <div>
      <h1>New user</h1>
      <form className="card" onSubmit={onSubmit} noValidate>
        {formError && (
          <div className="alert alert-error" role="alert">
            {formError}
          </div>
        )}

        <div className="field">
          <label htmlFor="name">Name</label>
          <input id="name" {...register("name")} />
          {errors.name && <p className="error">{errors.name.message}</p>}
        </div>

        <div className="field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            autoComplete="off"
            {...register("email")}
          />
          {errors.email && <p className="error">{errors.email.message}</p>}
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            {...register("password")}
          />
          {errors.password && (
            <p className="error">{errors.password.message}</p>
          )}
        </div>

        <div className="field">
          <label htmlFor="role">Role</label>
          <select id="role" {...register("role")}>
            <option value="User">User</option>
            <option value="Admin">Admin</option>
          </select>
          {errors.role && <p className="error">{errors.role.message}</p>}
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate("/admin/users")}
            disabled={isSubmitting}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="btn btn-primary btn-inline"
            disabled={isSubmitting}
          >
            {isSubmitting ? "Creating…" : "Create user"}
          </button>
        </div>
      </form>
    </div>
  );
}
