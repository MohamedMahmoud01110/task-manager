using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Infrastructure.Persistence;

public sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        db.Tasks.Add(task);
        await SaveAsync(ct);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> ListAsync(
        Guid userId, TaskListQuery q, CancellationToken ct)
    {
        var query = db.Tasks.AsNoTracking().Where(t => t.UserId == userId);

        if (q.Status is not null) query = query.Where(t => t.Status == q.Status);
        if (q.Priority is not null) query = query.Where(t => t.Priority == q.Priority);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            // NormalizedTitle is already trimmed + lower cased, so this is case-insensitive contains
            var term = TaskItem.NormalizeTitle(q.Search);
            query = query.Where(t => t.NormalizedTitle.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.Priority)   // High first
            .ThenBy(t => t.CreatedAt)             // oldest first
            .ThenBy(t => t.Id)                    // stable order so pages never overlap
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Remove(TaskItem task) => db.Tasks.Remove(task);

    public async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The task was changed by someone else. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(IndexNames.TaskTitlePerDayUnique))
        {
            throw new ConflictException("You already have a task with this title created today.");
        }
    }
}