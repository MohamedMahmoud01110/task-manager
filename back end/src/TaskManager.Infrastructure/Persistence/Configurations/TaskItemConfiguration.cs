// Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.ToTable("tasks");
        b.HasKey(t => t.Id);

        b.Property(t => t.Title).HasMaxLength(TaskItem.TitleMaxLength).IsRequired();
        b.Property(t => t.NormalizedTitle).HasMaxLength(TaskItem.TitleMaxLength).IsRequired();
        b.Property(t => t.Description).HasMaxLength(TaskItem.DescriptionMaxLength);
        b.Property(t => t.Status).HasConversion<int>();
        b.Property(t => t.Priority).HasConversion<int>();
        b.Property(t => t.CreatedDate).HasColumnType("date");

        // Optimistic concurrency token -> PostgreSQL system column xmin
        b.Property(t => t.Version).IsRowVersion();

        b.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);

        // THE duplicate-title rule, enforced by the database.
        b.HasIndex(t => new { t.UserId, t.NormalizedTitle, t.CreatedDate })
            .IsUnique()
            .HasDatabaseName(IndexNames.TaskTitlePerDayUnique);

        // Matches the list endpoint: filter by owner + status/priority
        b.HasIndex(t => new { t.UserId, t.Status, t.Priority });

        // Small partial index so the worker's "find Pending tasks" query on startup is cheap
        b.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_tasks_pending")
            .HasFilter("\"Status\" = 0");
    }
}