using TaskManager.Application.Tasks;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Enums.Enums;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.UnitTests;

public class TaskServiceTests
{
    private static readonly DateTime Now = new(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    private readonly FakeTaskRepository _repo = new();
    private readonly FakeTaskCache _cache = new();
    private readonly FakeTaskQueue _queue = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repo, _cache, _queue, new FakeClock(Now));
    }

    private static CreateTaskRequest NewRequest(string title = "Buy milk") =>
        new(title, null, TaskPriority.Medium, null);

    // ---------- Create + background queue ----------
    [Fact]
    public async Task Create_saves_the_task_and_enqueues_it_for_the_worker()
    {
        var dto = await _service.CreateAsync(Alice, NewRequest(), default);

        Assert.Contains(dto.Id, _repo.Items.Keys);
        Assert.Equal(new[] { dto.Id }, _queue.Enqueued);
        Assert.Equal(TaskItemStatus.Pending, dto.Status);
    }

    [Fact]
    public async Task Create_does_not_enqueue_when_saving_fails()
    {
        _repo.SaveException = new ConflictException("duplicate");

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(Alice, NewRequest(), default));

        Assert.Empty(_queue.Enqueued);
    }

    // ---------- Cache ----------
    [Fact]
    public async Task Second_get_is_served_from_cache_without_reading_the_database()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);

        await _service.GetAsync(Alice, created.Id, default);
        var callsAfterFirst = _repo.GetByIdCalls;
        await _service.GetAsync(Alice, created.Id, default);

        Assert.Equal(1, callsAfterFirst);
        Assert.Equal(callsAfterFirst, _repo.GetByIdCalls); // no extra DB read
    }

    [Fact]
    public async Task Another_user_never_gets_a_cached_task()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);
        await _service.GetAsync(Alice, created.Id, default); // now cached for Alice

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(Bob, created.Id, default));
    }

    [Fact]
    public async Task Cached_task_computes_overdue_at_read_time()
    {
        var due = Now.AddHours(1);
        var created = await _service.CreateAsync(Alice, new CreateTaskRequest("Due soon", null, TaskPriority.Low, due), default);
        await _service.GetAsync(Alice, created.Id, default);

        var later = new TaskService(_repo, _cache, _queue, new FakeClock(Now.AddHours(2)));
        var dto = await later.GetAsync(Alice, created.Id, default);

        Assert.True(dto.IsOverdue); // served from the cache, but not frozen at "false"
    }

    // ---------- Invalidation ----------
    [Fact]
    public async Task Update_invalidates_the_cache()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);
        await _service.GetAsync(Alice, created.Id, default);
        Assert.True(_cache.Contains(Alice, created.Id));

        await _service.UpdateAsync(Alice, created.Id,
            new UpdateTaskRequest("Buy bread", null, TaskPriority.High, null), default);

        Assert.False(_cache.Contains(Alice, created.Id));
        Assert.Contains((Alice, created.Id), _cache.Invalidations);
    }

    [Fact]
    public async Task Status_change_invalidates_the_cache()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);
        await _service.GetAsync(Alice, created.Id, default);

        await _service.ChangeStatusAsync(Alice, created.Id, new UpdateStatusRequest(TaskItemStatus.InProgress), default);

        Assert.False(_cache.Contains(Alice, created.Id));
    }

    [Fact]
    public async Task Delete_invalidates_the_cache()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);
        await _service.GetAsync(Alice, created.Id, default);

        await _service.DeleteAsync(Alice, created.Id, default);

        Assert.False(_cache.Contains(Alice, created.Id));
        Assert.DoesNotContain(created.Id, _repo.Items.Keys);
    }

    [Fact]
    public async Task Cache_is_kept_when_the_save_fails()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);
        await _service.GetAsync(Alice, created.Id, default);
        _repo.SaveException = new ConflictException("changed by someone else");

        await Assert.ThrowsAsync<ConflictException>(() => _service.ChangeStatusAsync(
            Alice, created.Id, new UpdateStatusRequest(TaskItemStatus.InProgress), default));

        Assert.Empty(_cache.Invalidations); // the database did not change, so the cache is still valid
    }

    // ---------- Ownership + workflow through the service ----------
    [Fact]
    public async Task Updating_or_deleting_someone_elses_task_is_not_found()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(
            Bob, created.Id, new UpdateTaskRequest("Hacked", null, TaskPriority.Low, null), default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Bob, created.Id, default));

        Assert.Contains(created.Id, _repo.Items.Keys);
        Assert.Empty(_cache.Invalidations);
    }

    [Fact]
    public async Task User_can_mark_a_pending_task_done_and_cannot_go_back()
    {
        var created = await _service.CreateAsync(Alice, NewRequest(), default);

        var done = await _service.ChangeStatusAsync(Alice, created.Id, new UpdateStatusRequest(TaskItemStatus.Done), default);
        Assert.Equal(TaskItemStatus.Done, done.Status);
        Assert.Empty(done.AllowedNextStatuses);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => _service.ChangeStatusAsync(
            Alice, created.Id, new UpdateStatusRequest(TaskItemStatus.Pending), default));
    }
}