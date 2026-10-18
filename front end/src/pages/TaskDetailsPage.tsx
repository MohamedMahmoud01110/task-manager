import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError } from "../api/client";
import { tasksApi } from "../api/tasks";
import {
  OverdueBadge,
  PriorityBadge,
  StatusBadge,
  statusLabel,
} from "../components/Badges";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { Spinner } from "../components/Spinner";
import { formatDateTime } from "../lib/dates";

const POLL_MS = 5000;

export function TaskDetailsPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [confirmOpen, setConfirmOpen] = useState(false);

  // Polling makes the worker's Pending -> InProgress change appear without a reload
  const {
    data: task,
    isPending,
    error,
    refetch,
  } = useQuery({
    queryKey: ["task", id],
    queryFn: () => tasksApi.get(id),
    refetchInterval: POLL_MS,
    refetchIntervalInBackground: false,
  });

  const changeStatus = useMutation({
    mutationFn: (status: Parameters<typeof tasksApi.changeStatus>[1]) =>
      tasksApi.changeStatus(id, status),
    onSuccess: (saved) => {
      queryClient.setQueryData(["task", id], saved);
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
    },
    // A 409 means the worker changed the task first: reload so the button shows the right next status
    onError: () => {
      refetch();
    },
  });

  const remove = useMutation({
    mutationFn: () => tasksApi.remove(id),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: ["task", id] });
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
      navigate("/tasks", { replace: true });
    },
  });

  if (isPending) return <Spinner />;

  if (error || !task) {
    const notFound = error instanceof ApiError && error.status === 404;
    return (
      <div className="card empty">
        <p>{notFound ? "Task not found." : "Failed to load the task."}</p>
        {!notFound && (
          <button
            className="btn btn-secondary btn-inline"
            onClick={() => refetch()}
          >
            Try again
          </button>
        )}{" "}
        <Link to="/tasks">Back to tasks</Link>
      </div>
    );
  }

  const allowed = task.allowedNextStatuses;
  return (
    <div>
      <p>
        <Link to="/tasks">← Back to tasks</Link>
      </p>

      <div className="card details">
        <div className="page-header">
          <h1>{task.title}</h1>
          <div className="task-badges">
            <PriorityBadge priority={task.priority} />
            <StatusBadge status={task.status} />
            {task.isOverdue && <OverdueBadge />}
          </div>
        </div>

        <dl className="fields">
          <dt>Description</dt>
          <dd>
            {task.description ?? <span className="muted">No description</span>}
          </dd>
          <dt>Due date</dt>
          <dd>
            {task.dueDate ? (
              formatDateTime(task.dueDate)
            ) : (
              <span className="muted">Not set</span>
            )}
          </dd>
          <dt>Created</dt>
          <dd>{formatDateTime(task.createdAt)}</dd>
          <dt>Last updated</dt>
          <dd>{formatDateTime(task.updatedAt)}</dd>
        </dl>

        {changeStatus.error && (
          <div className="alert alert-error" role="alert">
            {changeStatus.error.message}
          </div>
        )}

        <div className="form-actions spread">
          <div className="form-actions">
            {allowed.length > 0 ? (
              allowed.map((s, i) => (
                <button
                  key={s}
                  className={`btn btn-inline ${i === 0 ? "btn-primary" : "btn-secondary"}`}
                  disabled={changeStatus.isPending}
                  onClick={() => changeStatus.mutate(s)}
                >
                  {changeStatus.isPending && changeStatus.variables === s
                    ? "Updating…"
                    : s === "Done"
                      ? "Mark as Done"
                      : `Move to ${statusLabel(s)}`}
                </button>
              ))
            ) : (
              <span className="muted">This task is complete.</span>
            )}
          </div>
          <div className="form-actions">
            <Link className="btn btn-secondary" to={`/tasks/${task.id}/edit`}>
              Edit
            </Link>
            <button
              className="btn btn-danger"
              onClick={() => setConfirmOpen(true)}
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      <ConfirmDialog
        open={confirmOpen}
        title="Delete this task?"
        message={`"${task.title}" will be permanently deleted. This cannot be undone.`}
        confirmLabel="Delete"
        busy={remove.isPending}
        error={remove.error?.message}
        onConfirm={() => remove.mutate()}
        onCancel={() => {
          remove.reset();
          setConfirmOpen(false);
        }}
      />
    </div>
  );
}
