using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;
using static TaskManager.Domain.Enums.Enums;

namespace TaskManager.Infrastructure.Background;

public sealed class TaskProcessingWorker(
    ChannelTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ITaskCache cache,
    IOptions<WorkerOptions> options,
    TimeProvider clock,
    ILogger<TaskProcessingWorker> logger) : BackgroundService
{
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 1) The in-memory queue is empty after a restart, so refill it from the database
            await RecoverPendingTasksAsync(stoppingToken);

            // 2) Process queued tasks, a few at a time
            await Parallel.ForEachAsync(
                queue.Reader.ReadAllAsync(stoppingToken),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = options.Value.MaxConcurrency,
                    CancellationToken = stoppingToken
                },
                async (taskId, ct) => await ProcessSafelyAsync(taskId, ct));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
    }

    private async Task RecoverPendingTasksAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Uses the partial index ix_tasks_pending
            var ids = await db.Tasks.AsNoTracking()
                .Where(t => t.Status == TaskItemStatus.Pending)
                .OrderBy(t => t.CreatedAt)
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var id in ids) await queue.EnqueueAsync(id, ct);
            logger.LogInformation("Recovered {Count} pending task(s) after startup.", ids.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not recover pending tasks at startup.");
        }
    }

    private async Task ProcessSafelyAsync(Guid taskId, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.Value.ProcessingDelaySeconds), ct); // simulated work
            await MoveToInProgressAsync(taskId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down: the task is still Pending in the DB and will be recovered on the next start
        }
        catch (Exception ex)
        {
            // One bad task must never stop the worker
            logger.LogError(ex, "Failed to process task {TaskId}.", taskId);
        }
    }

    private async Task MoveToInProgressAsync(Guid taskId, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Fresh scope = fresh DbContext, so every attempt re-reads the row from the database
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);

            // Deleted, or the user already moved it forward: leave it alone
            if (task is null || task.Status != TaskItemStatus.Pending)
            {
                logger.LogInformation("Skipping task {TaskId}: deleted or no longer Pending.", taskId);
                return;
            }

            task.ChangeStatus(TaskItemStatus.InProgress, clock.GetUtcNow().UtcDateTime);

            try
            {
                // EF adds "AND xmin = <value we read>" to the UPDATE
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The row changed between our read and our write. Loop: re-read and re-decide.
                logger.LogInformation("Task {TaskId} changed concurrently (attempt {Attempt}). Re-checking.", taskId, attempt);
                continue;
            }

            await cache.InvalidateAsync(task.UserId, task.Id);
            logger.LogInformation("Task {TaskId} moved to InProgress by the worker.", taskId);
            return;
        }

        logger.LogWarning("Gave up on task {TaskId} after {Max} attempts.", taskId, MaxAttempts);
    }
}