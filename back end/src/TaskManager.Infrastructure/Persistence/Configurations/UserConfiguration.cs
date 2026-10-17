using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);

        b.Property(u => u.Name).HasMaxLength(100).IsRequired();
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.Property(u => u.NormalizedEmail).HasMaxLength(254).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(u => u.Role).HasConversion<int>();

        // Case-insensitive uniqueness: Ali@Example.com == ali@example.com
        b.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName(IndexNames.UserEmailUnique);

        // Soft delete: deleted users are invisible to every query by default
        b.HasQueryFilter(u => !u.IsDeleted);
    }
}