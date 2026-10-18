// src/components/TaskForm.tsx
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { Task, TaskInput } from "../api/types";
import { applyApiErrors } from "../lib/formErrors";
import { localInputToUtcIso, utcIsoToLocalInput } from "../lib/dates";

// Same rules as the API validators + the domain entity
const schema = z.object({
  title: z
    .string()
    .trim()
    .min(1, "Title is required.")
    .max(200, "Title cannot exceed 200 characters."),
  description: z
    .string()
    .max(2000, "Description cannot exceed 2000 characters."),
  priority: z.enum(["Low", "Medium", "High"]),
  dueDate: z.string(),
});
type FormValues = z.infer<typeof schema>;

interface Props {
  task?: Task; 
  submitLabel: string;
  onSubmit: (input: TaskInput) => Promise<void>;
  onCancel: () => void;
}

export function TaskForm({ task, submitLabel, onSubmit, onCancel }: Props) {
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: task?.title ?? "",
      description: task?.description ?? "",
      priority: task?.priority ?? "Medium",
      dueDate: utcIsoToLocalInput(task?.dueDate ?? null),
    },
  });

  const submit = handleSubmit(async (values) => {
    setFormError(null);

    // Client-side mirror of the "due date cannot be in the past" rule.
    // In edit mode an unchanged due date is allowed (the API allows it too).
    const dueIso = localInputToUtcIso(values.dueDate);
    const unchanged =
      task &&
      dueIso !== null &&
      new Date(dueIso).getTime() === new Date(task.dueDate ?? "").getTime();
    if (dueIso && !unchanged && new Date(dueIso).getTime() < Date.now()) {
      setError("dueDate", {
        type: "validate",
        message: "Due date cannot be in the past.",
      });
      return;
    }

    try {
      await onSubmit({
        title: values.title.trim(),
        description: values.description.trim() || null,
        priority: values.priority,
        dueDate: dueIso,
      });
    } catch (err) {
      // Input is kept; a 409 shows under the title field
      setFormError(
        applyApiErrors(
          err,
          setError,
          ["title", "description", "priority", "dueDate"],
          "title",
        ),
      );
    }
  });

  return (
    <form className="card" onSubmit={submit} noValidate>
      {formError && (
        <div className="alert alert-error" role="alert">
          {formError}
        </div>
      )}

      <div className="field">
        <label htmlFor="title">Title</label>
        <input
          id="title"
          maxLength={250}
          {...register("title")}
          aria-invalid={!!errors.title}
        />
        {errors.title && <p className="error">{errors.title.message}</p>}
      </div>

      <div className="field">
        <label htmlFor="description">Description</label>
        <textarea id="description" rows={4} {...register("description")} />
        {errors.description && (
          <p className="error">{errors.description.message}</p>
        )}
      </div>

      <div className="form-row">
        <div className="field">
          <label htmlFor="priority">Priority</label>
          <select id="priority" {...register("priority")}>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
          {errors.priority && (
            <p className="error">{errors.priority.message}</p>
          )}
        </div>

        <div className="field">
          <label htmlFor="dueDate">Due date (optional)</label>
          <input id="dueDate" type="datetime-local" {...register("dueDate")} />
          {errors.dueDate && <p className="error">{errors.dueDate.message}</p>}
        </div>
      </div>

      <div className="form-actions">
        <button
          type="button"
          className="btn btn-secondary"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </button>
        <button
          type="submit"
          className="btn btn-primary btn-inline"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Saving…" : submitLabel}
        </button>
      </div>
    </form>
  );
}
