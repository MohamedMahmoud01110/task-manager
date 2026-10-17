using System.Security.Claims;
using TaskManager.Application.Abstractions;

namespace TaskManager.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HTTP context.");

    public Guid Id => Guid.Parse(Principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public bool IsAdmin => Principal.IsInRole("Admin");
}