using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Infrastructure.Auth;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Missing Jwt configuration.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.Zero
                };

                o.Events = new JwtBearerEvents
                {
                    // Runs after the signature is valid. A valid token is NOT enough:
                    // the user must still exist and not be soft-deleted.
                    OnTokenValidated = async ctx =>
                    {
                        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                        // The soft-delete query filter hides deleted users, so AnyAsync is false for them
                        if (!Guid.TryParse(id, out var userId) ||
                            !await db.Users.AnyAsync(u => u.Id == userId, ctx.HttpContext.RequestAborted))
                        {
                            ctx.Fail("User no longer exists.");
                        }
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}