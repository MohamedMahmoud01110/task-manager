using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Enums.Enums;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Domain.Entities;

public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static User Create(string name, string email, string passwordHash, UserRole role, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainValidationException("Email is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = nowUtc
        };
    }

    public void SoftDelete(Guid actingUserId, DateTime nowUtc)
    {
        if (Id == actingUserId)
            throw new DomainValidationException("You cannot delete your own account.");
        IsDeleted = true;
        DeletedAt = nowUtc;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}