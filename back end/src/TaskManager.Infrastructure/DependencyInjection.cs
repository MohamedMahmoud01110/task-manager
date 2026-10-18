using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TaskManager.Application.Abstractions;
using TaskManager.Infrastructure.Auth;
using TaskManager.Infrastructure.Background;
using TaskManager.Infrastructure.Caching;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Default")));

        // Redis: the app must start and run even when Redis is down
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(config.GetConnectionString("Redis") ?? "localhost:6379");
            opts.AbortOnConnectFail = false;              // don't throw at startup if Redis is down; keep retrying in background
            opts.ConnectTimeout = 1000;
            opts.AsyncTimeout = 500;
            opts.SyncTimeout = 500;
            opts.BacklogPolicy = BacklogPolicy.FailFast;  // when disconnected, fail immediately instead of queueing commands
            return ConnectionMultiplexer.Connect(opts);
        });
        services.AddSingleton<ITaskCache, RedisTaskCache>();

        // Background processing
        services.Configure<WorkerOptions>(config.GetSection(WorkerOptions.Section));
        services.AddSingleton<ChannelTaskQueue>();
        services.AddSingleton<ITaskQueue>(sp => sp.GetRequiredService<ChannelTaskQueue>());
        services.AddHostedService<TaskProcessingWorker>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();


        return services;
    }
}