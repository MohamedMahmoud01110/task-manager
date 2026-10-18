import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { tasksApi } from "../api/tasks";
import { TaskForm } from "../components/TaskForm";

export function TaskCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const create = useMutation({
    mutationFn: tasksApi.create,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["tasks"] }),
  });

  return (
    <div>
      <h1>New task</h1>
      <TaskForm
        submitLabel="Create task"
        onCancel={() => navigate("/tasks")}
        onSubmit={async (input) => {
          const task = await create.mutateAsync(input);
          navigate(`/tasks/${task.id}`);
        }}
      />
    </div>
  );
}
