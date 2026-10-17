using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken ct);      // ConflictException on duplicate title
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> ListAsync(Guid userId, TaskListQuery query, CancellationToken ct);
    void Remove(TaskItem task);
    Task SaveAsync(CancellationToken ct);                    // ConflictException on duplicate title / concurrent change
}