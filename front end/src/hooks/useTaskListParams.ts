import { useCallback, useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import type { TaskListParams, TaskPriority, TaskStatus } from "../api/types";

const STATUSES: TaskStatus[] = ["Pending", "InProgress", "Done"];
const PRIORITIES: TaskPriority[] = ["Low", "Medium", "High"];
export const PAGE_SIZE = 10;

/** The URL is the single source of truth for page, filters and search. */
export function useTaskListParams() {
  const [searchParams, setSearchParams] = useSearchParams();

  const params = useMemo<TaskListParams>(() => {
    const status = searchParams.get("status");
    const priority = searchParams.get("priority");
    const page = Number(searchParams.get("page"));
    return {
      page: Number.isInteger(page) && page >= 1 ? page : 1,
      pageSize: PAGE_SIZE,
      status: STATUSES.includes(status as TaskStatus)
        ? (status as TaskStatus)
        : undefined,
      priority: PRIORITIES.includes(priority as TaskPriority)
        ? (priority as TaskPriority)
        : undefined,
      search: searchParams.get("search") ?? "",
    };
  }, [searchParams]);

  const update = useCallback(
    (
      changes: Partial<
        Record<
          "status" | "priority" | "search" | "page",
          string | number | undefined
        >
      >,
    ) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          for (const [key, value] of Object.entries(changes)) {
            if (
              value === undefined ||
              value === "" ||
              (key === "page" && value === 1)
            )
              next.delete(key);
            else next.set(key, String(value));
          }
          // Any filter change goes back to page 1, unless the page itself is what changed
          if (!("page" in changes)) next.delete("page");
          return next;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  return { params, update };
}
