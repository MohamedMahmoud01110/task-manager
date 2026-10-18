namespace TaskManager.Application.Abstractions;

public interface ITaskQueue
{
    ValueTask EnqueueAsync(Guid taskId, CancellationToken ct = default);
}