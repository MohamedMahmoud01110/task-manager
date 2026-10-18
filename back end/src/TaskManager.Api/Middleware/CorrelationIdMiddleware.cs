namespace TaskManager.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = ctx.Request.Headers[HeaderName].FirstOrDefault();

        // Accept the caller's id only if it is short and harmless (it ends up in logs and headers)
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || !id.All(c => char.IsLetterOrDigit(c) || c == '-'))
            id = Guid.NewGuid().ToString("N");

        ctx.TraceIdentifier = id;                 // ProblemDetails "traceId" now equals the correlation id
        ctx.Response.Headers[HeaderName] = id;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(ctx);
        }
    }
}