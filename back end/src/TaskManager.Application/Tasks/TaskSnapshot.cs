using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using static TaskManager.Domain.Enums.Enums;

namespace TaskManager.Application.Tasks;

/// <summary>Raw task state. This is what we cache. Time-dependent fields are computed on read.</summary>
public sealed record TaskSnapshot(
    Guid Id, Guid UserId, string Title, string? Description,
    TaskItemStatus Status, TaskPriority Priority,
    DateTime? DueDate, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static TaskSnapshot From(TaskItem t) => new(
        t.Id, t.UserId, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.CreatedAt, t.UpdatedAt);

    public TaskDto ToDto(DateTime nowUtc) => new(
        Id, Title, Description, Status, Priority, DueDate, CreatedAt, UpdatedAt,
        TaskItem.IsOverdue(DueDate, Status, nowUtc),
                 TaskItem.AllowedNextFor(Status));
}