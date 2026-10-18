using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace TaskManager.IntegrationTests;

/// <summary>Starts a throw-away PostgreSQL container and the real API on top of it.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Environment variables are read by Program.cs before the app is built, so they reliably win
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _db.GetConnectionString());
        // Nothing listens here: every test runs with Redis DOWN, which also proves the fallback
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "localhost:6399");
        Environment.SetEnvironmentVariable("Worker__ProcessingDelaySeconds", "2");
        Environment.SetEnvironmentVariable("RateLimiting__LoginPerMinute", "1000");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}