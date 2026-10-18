using System.Threading.Channels;
using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Background;

public sealed class ChannelTaskQueue : ITaskQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(Guid taskId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(taskId, ct);
}