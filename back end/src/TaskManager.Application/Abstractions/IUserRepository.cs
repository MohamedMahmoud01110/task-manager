using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);   // normalized lookup, ignores deleted
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<User>> ListAsync(CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);                    // throws ConflictException on duplicate email
    Task SaveAsync(CancellationToken ct);
}