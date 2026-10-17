using TaskManager.Domain.Entities;

namespace TaskManager.Application.Abstractions;

public record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}