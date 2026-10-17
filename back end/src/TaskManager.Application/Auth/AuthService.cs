using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Enums.Enums;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Application.Auth;

public sealed class AuthService(
    IUserRepository users, IPasswordHasher hasher, ITokenService tokens, TimeProvider clock)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest r, CancellationToken ct)
    {
        // Public registration always creates a normal user. Role is never taken from the client.
        var user = await CreateAsync(r.Name, r.Email, r.Password, UserRole.User, ct);
        return Issue(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(r.Email, ct);

        // Same message for "no such user" and "wrong password" so we never reveal which emails exist
        if (user is null || !hasher.Verify(r.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return Issue(user);
    }

    public async Task<UserDto> CreateByAdminAsync(CreateUserRequest r, CancellationToken ct)
        => ToDto(await CreateAsync(r.Name, r.Email, r.Password, r.Role, ct));

    public async Task<UserDto> GetProfileAsync(Guid id, CancellationToken ct)
        => ToDto(await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found."));

    private async Task<User> CreateAsync(string name, string email, string password, UserRole role, CancellationToken ct)
    {
        if (await users.GetByEmailAsync(email, ct) is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = User.Create(name, email, hasher.Hash(password), role, clock.GetUtcNow().UtcDateTime);
        await users.AddAsync(user, ct); // the unique index still protects us from concurrent registers
        return user;
    }

    private AuthResponse Issue(User user)
    {
        var t = tokens.CreateToken(user);
        return new AuthResponse(t.Value, t.ExpiresAtUtc, ToDto(user));
    }

    public static UserDto ToDto(User u) => new(u.Id, u.Name, u.Email, u.Role, u.CreatedAt);
}