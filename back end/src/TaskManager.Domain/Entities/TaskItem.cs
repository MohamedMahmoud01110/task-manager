using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Enums.Enums;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Domain.Entities;

public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string NormalizedTitle { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public uint Version { get; private set; } // optimistic concurrency token (maps to PostgreSQL xmin)
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateOnly CreatedDate { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UserId { get; private set; }

    public static TaskItem Create(
        Guid userId, string title, string? description,
        TaskPriority priority, DateTime? dueDateUtc, DateTime nowUtc)
    {
        var cleanTitle = ValidateTitle(title);
        EnsureDueDateNotInPast(dueDateUtc, nowUtc);

        return new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = cleanTitle,
            NormalizedTitle = NormalizeTitle(cleanTitle),
            Description = CleanDescription(description),
            Status = TaskItemStatus.Pending,
            Priority = priority,
            DueDate = dueDateUtc,
            CreatedAt = nowUtc,
            CreatedDate = DateOnly.FromDateTime(nowUtc),
            UpdatedAt = nowUtc
        };
    }

    public void UpdateDetails(
        string title, string? description, TaskPriority priority,
        DateTime? dueDateUtc, DateTime nowUtc)
    {
        var cleanTitle = ValidateTitle(title);

        // Only validate the due date if it is being changed, so a user can still
        // edit the description of a task that has since become overdue.
        if (dueDateUtc != DueDate)
            EnsureDueDateNotInPast(dueDateUtc, nowUtc);

        Title = cleanTitle;
        NormalizedTitle = NormalizeTitle(cleanTitle);
        Description = CleanDescription(description);
        Priority = priority;
        DueDate = dueDateUtc;
        UpdatedAt = nowUtc;
    }

    public void ChangeStatus(TaskItemStatus next, DateTime nowUtc)
    {
        if (NextAllowedStatus != next)
            throw new InvalidStatusTransitionException(Status, next);

        Status = next;
        UpdatedAt = nowUtc;
    }

    /// <summary>The only status this task may move to (null when Done). The UI uses this too.</summary>
    public TaskItemStatus? NextAllowedStatus => Status switch
    {
        TaskItemStatus.Pending => TaskItemStatus.InProgress,
        TaskItemStatus.InProgress => TaskItemStatus.Done,
        _ => null
    };

    public bool IsOverdue(DateTime nowUtc) =>
        DueDate is not null && DueDate < nowUtc && Status != TaskItemStatus.Done;

    /// <summary>Ownership check. Returns 404 (not 403) so we never reveal that the task exists.</summary>
    public void EnsureOwnedBy(Guid userId)
    {
        if (UserId != userId)
            throw new NotFoundException("Task not found.");
    }

    // ---- helpers ----
    public static string NormalizeTitle(string title) => title.Trim().ToLowerInvariant();

    private static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainValidationException("Title is required and cannot be only spaces.");
        if (trimmed.Length > TitleMaxLength)
            throw new DomainValidationException($"Title cannot exceed {TitleMaxLength} characters.");
        return trimmed;
    }

    private static string? CleanDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (trimmed is { Length: > DescriptionMaxLength })
            throw new DomainValidationException($"Description cannot exceed {DescriptionMaxLength} characters.");
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static void EnsureDueDateNotInPast(DateTime? dueDateUtc, DateTime nowUtc)
    {
        if (dueDateUtc is not null && dueDateUtc < nowUtc)
            throw new DomainValidationException("Due date cannot be in the past.");
    }
}