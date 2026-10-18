using TaskManager.Application.Tasks;

namespace TaskManager.Application.Abstractions;

/// <summary>Implementations must never throw: a cache problem is a cache miss, not an error.</summary>
public interface ITaskCache
{
    Task<TaskSnapshot?> GetAsync(Guid userId, Guid taskId);
    Task SetAsync(TaskSnapshot task);
    Task InvalidateAsync(Guid userId, Guid taskId);
}