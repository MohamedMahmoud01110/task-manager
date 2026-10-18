// src/pages/TasksPage.tsx
import { useEffect, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { tasksApi } from "../api/tasks";
import type { TaskPriority, TaskStatus } from "../api/types";
import { OverdueBadge, PriorityBadge, StatusBadge } from "../components/Badges";
import { Pagination } from "../components/Pagination";
import { Spinner } from "../components/Spinner";
import { useDebouncedValue } from "../hooks/useDebouncedValue";
import { useTaskListParams } from "../hooks/useTaskListParams";

const POLL_MS = 5000;

export function TasksPage() {
  const { params, update } = useTaskListParams();

  // The input has its own state so typing is instant; the URL (and the API call) follow 400ms later
  const [searchText, setSearchText] = useState(params.search ?? "");
  const debouncedSearch = useDebouncedValue(searchText, 400);

  useEffect(() => {
    if (debouncedSearch.trim() !== (params.search ?? "")) {
      update({ search: debouncedSearch.trim() });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);

  // Keep the input in sync when the URL changes from outside (back button, shared link)
  useEffect(() => {
    setSearchText(params.search ?? "");
  }, [params.search]);

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: ["tasks", params],
    queryFn: ({ signal }) => tasksApi.list(params, signal),
    placeholderData: keepPreviousData,
    refetchInterval: POLL_MS,
    refetchIntervalInBackground: false,
  });

  const hasFilters = Boolean(params.status || params.priority || params.search);

  return (
    <div>
      <div className="page-header">
        <h1>My tasks</h1>
        <Link className="btn btn-primary btn-inline" to="/tasks/new">
          New task
        </Link>
      </div>

      <div className="card filters">
        <input
          type="search"
          placeholder="Search by title…"
          aria-label="Search by title"
          value={searchText}
          maxLength={200}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <select
          aria-label="Filter by status"
          value={params.status ?? ""}
          onChange={(e) =>
            update({
              status: (e.target.value || undefined) as TaskStatus | undefined,
            })
          }
        >
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="InProgress">In progress</option>
          <option value="Done">Done</option>
        </select>
        <select
          aria-label="Filter by priority"
          value={params.priority ?? ""}
          onChange={(e) =>
            update({
              priority: (e.target.value || undefined) as
                | TaskPriority
                | undefined,
            })
          }
        >
          <option value="">All priorities</option>
          <option value="High">High</option>
          <option value="Medium">Medium</option>
          <option value="Low">Low</option>
        </select>
        {hasFilters && (
          <button
            className="btn btn-secondary"
            onClick={() => {
              setSearchText("");
              update({
                status: undefined,
                priority: undefined,
                search: undefined,
              });
            }}
          >
            Clear
          </button>
        )}
      </div>

      {isPending && <Spinner />}

      {isError && !data && (
        <div className="alert alert-error" role="alert">
          {error instanceof Error ? error.message : "Failed to load tasks."}{" "}
          <button className="link-btn" onClick={() => refetch()}>
            Try again
          </button>
        </div>
      )}

      {data && data.items.length === 0 && (
        <div className="card empty">
          {hasFilters ? (
            <p>No tasks match your filters.</p>
          ) : (
            <>
              <p>You have no tasks yet.</p>
              <Link className="btn btn-primary btn-inline" to="/tasks/new">
                Create your first task
              </Link>
            </>
          )}
        </div>
      )}

      {data && data.items.length > 0 && (
        <>
          <ul className={`task-list${isFetching ? " is-fetching" : ""}`}>
            {data.items.map((t) => (
              <li key={t.id} className="card task-row">
                <div className="task-main">
                  <Link to={`/tasks/${t.id}`} className="task-title">
                    {t.title}
                  </Link>
                  {t.dueDate && (
                    <span className="muted small">
                      Due {new Date(t.dueDate).toLocaleDateString()}
                    </span>
                  )}
                </div>
                <div className="task-badges">
                  <PriorityBadge priority={t.priority} />
                  <StatusBadge status={t.status} />
                  {t.isOverdue && <OverdueBadge />}
                </div>
              </li>
            ))}
          </ul>

          <p className="muted small">
            {data.totalCount} task{data.totalCount === 1 ? "" : "s"} in total
          </p>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            onChange={(page) => update({ page })}
          />
        </>
      )}
    </div>
  );
}
