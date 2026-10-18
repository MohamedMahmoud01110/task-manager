import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { adminUsersApi } from "../api/adminUsers";
import type { User } from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { Spinner } from "../components/Spinner";
import { formatDateTime } from "../lib/dates";

export function AdminUsersPage() {
  const { user: me } = useAuth();
  const queryClient = useQueryClient();
  const [toDelete, setToDelete] = useState<User | null>(null);

  const { data, isPending, isError, error, refetch } = useQuery({
    queryKey: ["admin-users"],
    queryFn: adminUsersApi.list,
  });

  const remove = useMutation({
    mutationFn: (id: string) => adminUsersApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      setToDelete(null);
    },
  });

  const closeDialog = () => {
    remove.reset();
    setToDelete(null);
  };

  return (
    <div>
      <div className="page-header">
        <h1>Users</h1>
        <Link className="btn btn-primary btn-inline" to="/admin/users/new">
          New user
        </Link>
      </div>

      {isPending && <Spinner />}

      {isError && (
        <div className="alert alert-error" role="alert">
          {error instanceof Error ? error.message : "Failed to load users."}{" "}
          <button className="link-btn" onClick={() => refetch()}>
            Try again
          </button>
        </div>
      )}

      {data && data.length === 0 && (
        <div className="card empty">
          <p>No users found.</p>
        </div>
      )}

      {data && data.length > 0 && (
        <div className="table-wrap card">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Created</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {data.map((u) => {
                const isSelf = u.id === me?.id;
                return (
                  <tr key={u.id}>
                    <td data-label="Name">
                      {u.name}
                      {isSelf && <span className="muted small"> (you)</span>}
                    </td>
                    <td data-label="Email">{u.email}</td>
                    <td data-label="Role">
                      <span className="badge">{u.role}</span>
                    </td>
                    <td data-label="Created">{formatDateTime(u.createdAt)}</td>
                    <td className="actions">
                      <button
                        className="btn btn-danger"
                        disabled={isSelf}
                        title={
                          isSelf
                            ? "You cannot delete your own account"
                            : undefined
                        }
                        onClick={() => setToDelete(u)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog
        open={toDelete !== null}
        title="Delete this user?"
        message={`${toDelete?.name ?? ""} (${toDelete?.email ?? ""}) will no longer be able to sign in. Their existing sessions stop working immediately.`}
        confirmLabel="Delete user"
        busy={remove.isPending}
        error={remove.error?.message}
        onConfirm={() => toDelete && remove.mutate(toDelete.id)}
        onCancel={closeDialog}
      />
    </div>
  );
}
