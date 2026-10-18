import type { TaskPriority, TaskStatus } from "../api/types";

const STATUS_LABEL: Record<TaskStatus, string> = {
  Pending: "Pending",
  InProgress: "In progress",
  Done: "Done",
};

export const statusLabel = (s: TaskStatus) => STATUS_LABEL[s];

export function StatusBadge({ status }: { status: TaskStatus }) {
  return (
    <span className={`badge status-${status}`}>{STATUS_LABEL[status]}</span>
  );
}

export function PriorityBadge({ priority }: { priority: TaskPriority }) {
  return <span className={`badge priority-${priority}`}>{priority}</span>;
}

export function OverdueBadge() {
  return <span className="badge overdue">Overdue</span>;
}
