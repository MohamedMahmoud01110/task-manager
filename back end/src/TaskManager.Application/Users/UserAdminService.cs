using TaskManager.Application.Abstractions;
using TaskManager.Application.Auth;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Application.Users;

public sealed class UserAdminService(IUserRepository users, AuthService auth, TimeProvider clock)
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct)
        => (await users.ListAsync(ct)).Select(AuthService.ToDto).ToList();

    public Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
        => auth.CreateByAdminAsync(request, ct);

    public async Task DeleteAsync(Guid actingAdminId, Guid targetId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(targetId, ct)
                   ?? throw new NotFoundException("User not found.");

        // The "cannot delete yourself" rule lives in the domain entity
        user.SoftDelete(actingAdminId, clock.GetUtcNow().UtcDateTime);
        await users.SaveAsync(ct);
    }
}