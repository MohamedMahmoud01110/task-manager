// tests/TaskManager.UnitTests/TaskItemTests.cs
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Enums.Enums;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.UnitTests;

public class TaskItemTests
{
    private static readonly DateTime Now = new(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.NewGuid();

    private static TaskItem NewTask(string title = "Buy milk", DateTime? due = null) =>
        TaskItem.Create(Owner, title, null, TaskPriority.Medium, due, Now);

    // ---------- Duplicate-title key ----------
    [Theory]
    [InlineData("Buy creatine", "buy creatine")]
    [InlineData("  Buy creatine  ", "BUY CREATINE")]
    public void Titles_differing_only_by_case_or_edge_spaces_produce_the_same_key(string a, string b)
    {
        Assert.Equal(NewTask(a).NormalizedTitle, NewTask(b).NormalizedTitle);
    }

    [Fact]
    public void Same_title_on_different_utc_days_produces_different_keys()
    {
        var today = TaskItem.Create(Owner, "Buy milk", null, TaskPriority.Low, null, Now);
        var tomorrow = TaskItem.Create(Owner, "Buy milk", null, TaskPriority.Low, null, Now.AddDays(1));
        Assert.NotEqual(today.CreatedDate, tomorrow.CreatedDate);
    }

    [Fact]
    public void Utc_day_boundary_is_respected()
    {
        var late = new DateTime(2025, 6, 10, 23, 59, 59, DateTimeKind.Utc);
        var early = new DateTime(2025, 6, 11, 0, 0, 1, DateTimeKind.Utc);
        var a = TaskItem.Create(Owner, "x", null, TaskPriority.Low, null, late);
        var b = TaskItem.Create(Owner, "x", null, TaskPriority.Low, null, early);
        Assert.NotEqual(a.CreatedDate, b.CreatedDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void Title_with_only_spaces_is_rejected(string title)
    {
        Assert.Throws<DomainValidationException>(() => NewTask(title));
    }

    // ---------- Status workflow ----------
    private static TaskItem TaskIn(TaskItemStatus status)
    {
        var task = NewTask();
        if (status != TaskItemStatus.Pending) task.ChangeStatus(status, Now);
        return task;
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Done)] // user finishes before the worker runs
    public void Forward_transitions_are_allowed(TaskItemStatus from, TaskItemStatus to)
    {
        var task = TaskIn(from);
        task.ChangeStatus(to, Now);
        Assert.Equal(to, task.Status);
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Pending)]
    public void Backward_or_repeated_transitions_are_rejected(TaskItemStatus from, TaskItemStatus to)
    {
        var task = TaskIn(from);
        Assert.Throws<InvalidStatusTransitionException>(() => task.ChangeStatus(to, Now));
        Assert.Equal(from, task.Status); // unchanged
    }

    [Fact]
    public void Undefined_status_value_is_rejected()
    {
        var task = NewTask();
        Assert.Throws<InvalidStatusTransitionException>(() => task.ChangeStatus((TaskItemStatus)99, Now));
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, new[] { TaskItemStatus.InProgress, TaskItemStatus.Done })]
    [InlineData(TaskItemStatus.InProgress, new[] { TaskItemStatus.Done })]
    [InlineData(TaskItemStatus.Done, new TaskItemStatus[0])]
    public void Allowed_next_statuses_match_the_workflow(TaskItemStatus current, TaskItemStatus[] expected)
    {
        Assert.Equal(expected, TaskIn(current).AllowedNextStatuses);
    }

    // ---------- Due date ----------
    [Fact]
    public void Creating_with_past_due_date_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() => NewTask(due: Now.AddMinutes(-1)));
    }

    [Fact]
    public void Updating_to_a_past_due_date_is_rejected()
    {
        var task = NewTask(due: Now.AddDays(1));
        Assert.Throws<DomainValidationException>(() =>
            task.UpdateDetails("Buy milk", null, TaskPriority.Low, Now.AddDays(-1), Now));
    }

    [Fact]
    public void Editing_other_fields_of_an_overdue_task_is_allowed_when_due_date_is_unchanged()
    {
        var task = NewTask(due: Now.AddHours(1));
        var later = Now.AddDays(2); // task is now overdue
        task.UpdateDetails("New title", "desc", TaskPriority.High, task.DueDate, later);
        Assert.Equal("New title", task.Title);
    }

    [Fact]
    public void Overdue_means_past_due_and_not_done()
    {
        var task = NewTask(due: Now.AddHours(1));
        Assert.False(task.IsOverdue(Now));
        Assert.True(task.IsOverdue(Now.AddHours(2)));

        task.ChangeStatus(TaskItemStatus.InProgress, Now);
        task.ChangeStatus(TaskItemStatus.Done, Now);
        Assert.False(task.IsOverdue(Now.AddHours(2))); // Done is never overdue
    }

    // ---------- Ownership ----------
    [Fact]
    public void Owner_passes_the_ownership_check()
    {
        NewTask().EnsureOwnedBy(Owner); // no exception
    }

    [Fact]
    public void Other_user_gets_not_found()
    {
        Assert.Throws<NotFoundException>(() => NewTask().EnsureOwnedBy(Guid.NewGuid()));
    }
}