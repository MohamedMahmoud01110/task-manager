using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Application.Tasks;

public sealed class TaskService(ITaskRepository repo, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequest r, CancellationToken ct)
    {
        var task = TaskItem.Create(userId, r.Title, r.Description, r.Priority, r.DueDate.ToUtc(), Now);
        await repo.AddAsync(task, ct);
        // TODO (worker step): enqueue task.Id for background processing here
        return ToDto(task);
    }

    public async Task<TaskDto> GetAsync(Guid userId, Guid id, CancellationToken ct)
        => ToDto(await GetOwnedAsync(userId, id, ct));

    public async Task<PagedResult<TaskDto>> ListAsync(Guid userId, TaskListQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.ListAsync(userId, q, ct);
        var pages = (int)Math.Ceiling(total / (double)q.PageSize);
        return new PagedResult<TaskDto>(items.Select(ToDto).ToList(), q.Page, q.PageSize, total, pages);
    }

    public async Task<TaskDto> UpdateAsync(Guid userId, Guid id, UpdateTaskRequest r, CancellationToken ct)
    {
        var task = await GetOwnedAsync(userId, id, ct);
        task.UpdateDetails(r.Title, r.Description, r.Priority, r.DueDate.ToUtc(), Now);
        await repo.SaveAsync(ct);
        // TODO (redis step): invalidate cache for this task
        return ToDto(task);
    }

    public async Task<TaskDto> ChangeStatusAsync(Guid userId, Guid id, UpdateStatusRequest r, CancellationToken ct)
    {
        var task = await GetOwnedAsync(userId, id, ct);
        task.ChangeStatus(r.Status, Now);
        await repo.SaveAsync(ct);
        // TODO (redis step): invalidate cache for this task
        return ToDto(task);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var task = await GetOwnedAsync(userId, id, ct);
        repo.Remove(task);
        await repo.SaveAsync(ct);
        // TODO (redis step): invalidate cache for this task
    }

    // Ownership is enforced by the domain entity. A foreign task looks exactly like a missing one (404).
    private async Task<TaskItem> GetOwnedAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Task not found.");
        task.EnsureOwnedBy(userId);
        return task;
    }

    private TaskDto ToDto(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
        t.CreatedAt, t.UpdatedAt, t.IsOverdue(Now), t.NextAllowedStatus);
}