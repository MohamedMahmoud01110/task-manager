using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks;

namespace TaskManager.Infrastructure.Caching;

public sealed class RedisTaskCache(IConnectionMultiplexer redis, ILogger<RedisTaskCache> logger) : ITaskCache
{
    // Safety net: even if an invalidation is ever missed, stale data lives at most this long
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    private static string Key(Guid userId, Guid taskId) => $"task:{userId:N}:{taskId:N}";

    public async Task<TaskSnapshot?> GetAsync(Guid userId, Guid taskId)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(Key(userId, taskId));
            return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TaskSnapshot>(value.ToString());
        }
        catch (Exception ex)
        {
            Warn("GET", ex);
            return null;
        }
    }

    public async Task SetAsync(TaskSnapshot task)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(
                Key(task.UserId, task.Id), JsonSerializer.Serialize(task), Ttl);
        }
        catch (Exception ex) { Warn("SET", ex); }
    }

    public async Task InvalidateAsync(Guid userId, Guid taskId)
    {
        try { await redis.GetDatabase().KeyDeleteAsync(Key(userId, taskId)); }
        catch (Exception ex) { Warn("DELETE", ex); }
    }

    private void Warn(string operation, Exception ex) =>
        logger.LogWarning("Redis unavailable during {Operation}. Falling back to the database. Reason: {Reason}",
            operation, ex.Message);
}