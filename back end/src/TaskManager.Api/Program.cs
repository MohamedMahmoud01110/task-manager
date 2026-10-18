using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using TaskManager.Api.Extensions;
using TaskManager.Api.Middleware;
using TaskManager.Application.Abstractions;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddSwaggerWithJwt();
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                  ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var loginLimit = builder.Configuration.GetValue("RateLimiting:LoginPerMinute", 10);

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // One bucket per client IP
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = loginLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Same ProblemDetails shape as every other error
    o.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 429,
            Title = "Too many requests",
            Detail = "Too many login attempts. Please wait a minute and try again.",
            Instance = context.HttpContext.Request.Path
        }, options: null, contentType: "application/problem+json", cancellationToken: ct);
    };
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();

await DbSeeder.SeedAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }