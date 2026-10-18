import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError } from "../api/client";
import { tasksApi } from "../api/tasks";
import { Spinner } from "../components/Spinner";
import { TaskForm } from "../components/TaskForm";

export function TaskEditPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    data: task,
    isPending,
    error,
  } = useQuery({
    queryKey: ["task", id],
    queryFn: () => tasksApi.get(id),
  });

  const update = useMutation({
    mutationFn: (input: Parameters<typeof tasksApi.update>[1]) =>
      tasksApi.update(id, input),
    onSuccess: (saved) => {
      queryClient.setQueryData(["task", id], saved);
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
    },
  });

  if (isPending) return <Spinner />;

  if (error || !task) {
    const notFound = error instanceof ApiError && error.status === 404;
    return (
      <div className="card empty">
        <p>{notFound ? "Task not found." : "Failed to load the task."}</p>
        <Link to="/tasks">Back to tasks</Link>
      </div>
    );
  }

  return (
    <div>
      <h1>Edit task</h1>
      <TaskForm
        task={task}
        submitLabel="Save changes"
        onCancel={() => navigate(`/tasks/${id}`)}
        onSubmit={async (input) => {
          await update.mutateAsync(input);
          navigate(`/tasks/${id}`);
        }}
      />
    </div>
  );
}
