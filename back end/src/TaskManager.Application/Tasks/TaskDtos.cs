using FluentValidation;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using static TaskManager.Domain.Enums.Enums;

namespace TaskManager.Application.Tasks;

public record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);
public record UpdateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);
public record UpdateStatusRequest(TaskItemStatus Status);

public record TaskDto(
    Guid Id, string Title, string? Description,
    TaskItemStatus Status, TaskPriority Priority,
    DateTime? DueDate, DateTime CreatedAt, DateTime UpdatedAt,
    bool IsOverdue, TaskItemStatus? NextAllowedStatus);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public class TaskListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? Search { get; set; }
}

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.")
            .Must(t => t is null || t.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"Title cannot exceed {TaskItem.TitleMaxLength} characters.");
        RuleFor(x => x.Description).MaximumLength(TaskItem.DescriptionMaxLength);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.")
            .Must(t => t is null || t.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"Title cannot exceed {TaskItem.TitleMaxLength} characters.");
        RuleFor(x => x.Description).MaximumLength(TaskItem.DescriptionMaxLength);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}

public class TaskListQueryValidator : AbstractValidator<TaskListQuery>
{
    public TaskListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(TaskItem.TitleMaxLength);
    }
}