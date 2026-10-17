namespace TaskManager.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; init; } = "TaskManager";
    public string Audience { get; init; } = "TaskManager.Client";
    public string Key { get; init; } = null!;          // at least 32 characters
    public int ExpiryMinutes { get; init; } = 60;
}