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
    [InlineData("Buy milk", "buy milk")]
    [InlineData("  Buy milk  ", "BUY MILK")]
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
    [Fact]
    public void Status_moves_forward_through_the_workflow()
    {
        var task = NewTask();
        task.ChangeStatus(TaskItemStatus.InProgress, Now);
        task.ChangeStatus(TaskItemStatus.Done, Now);
        Assert.Equal(TaskItemStatus.Done, task.Status);
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Done)]   // skipping is not allowed
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Pending)]
    public void Invalid_transitions_are_rejected(TaskItemStatus from, TaskItemStatus to)
    {
        var task = NewTask();
        if (from >= TaskItemStatus.InProgress) task.ChangeStatus(TaskItemStatus.InProgress, Now);
        if (from == TaskItemStatus.Done) task.ChangeStatus(TaskItemStatus.Done, Now);

        Assert.Throws<InvalidStatusTransitionException>(() => task.ChangeStatus(to, Now));
        Assert.Equal(from, task.Status); // unchanged
    }

    [Fact]
    public void Done_task_has_no_next_status()
    {
        var task = NewTask();
        task.ChangeStatus(TaskItemStatus.InProgress, Now);
        task.ChangeStatus(TaskItemStatus.Done, Now);
        Assert.Null(task.NextAllowedStatus);
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