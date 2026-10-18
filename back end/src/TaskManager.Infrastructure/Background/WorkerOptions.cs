namespace TaskManager.Infrastructure.Background;

public sealed class WorkerOptions
{
    public const string Section = "Worker";
    public int ProcessingDelaySeconds { get; init; } = 5;
    public int MaxConcurrency { get; init; } = 4;
}
