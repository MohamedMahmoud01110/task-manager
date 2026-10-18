using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.UnitTests;

public sealed class FakeClock(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
}

public sealed class FakeTaskRepository : ITaskRepository
{
    public Dictionary<Guid, TaskItem> Items { get; } = new();
    public int GetByIdCalls { get; private set; }
    public Exception? SaveException { get; set; }

    public Task AddAsync(TaskItem task, CancellationToken ct)
    {
        if (SaveException is not null) throw SaveException;
        Items[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        GetByIdCalls++;
        return Task.FromResult(Items.GetValueOrDefault(id));
    }

    public Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> ListAsync(Guid userId, TaskListQuery query, CancellationToken ct)
        => Task.FromResult(((IReadOnlyList<TaskItem>)Items.Values.ToList(), Items.Count));

    public void Remove(TaskItem task) => Items.Remove(task.Id);

    public Task SaveAsync(CancellationToken ct)
        => SaveException is null ? Task.CompletedTask : throw SaveException;
}

public sealed class FakeTaskCache : ITaskCache
{
    private readonly Dictionary<(Guid User, Guid Task), TaskSnapshot> _store = new();
    public List<(Guid User, Guid Task)> Invalidations { get; } = new();

    public bool Contains(Guid userId, Guid taskId) => _store.ContainsKey((userId, taskId));

    public Task<TaskSnapshot?> GetAsync(Guid userId, Guid taskId)
        => Task.FromResult(_store.GetValueOrDefault((userId, taskId)));

    public Task SetAsync(TaskSnapshot task)
    {
        _store[(task.UserId, task.Id)] = task;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid userId, Guid taskId)
    {
        Invalidations.Add((userId, taskId));
        _store.Remove((userId, taskId));
        return Task.CompletedTask;
    }
}

public sealed class FakeTaskQueue : ITaskQueue
{
    public List<Guid> Enqueued { get; } = new();

    public ValueTask EnqueueAsync(Guid taskId, CancellationToken ct = default)
    {
        Enqueued.Add(taskId);
        return ValueTask.CompletedTask;
    }
}