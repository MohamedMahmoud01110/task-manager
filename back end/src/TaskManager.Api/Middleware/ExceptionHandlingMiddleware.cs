using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaskManager.Domain.Exceptions;
using static TaskManager.Domain.Exceptions.DomainExceptions;

namespace TaskManager.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex) { await WriteAsync(ctx, ex); }
    }

    private async Task WriteAsync(HttpContext ctx, Exception ex)
    {
        if (ctx.Response.HasStarted) throw ex;

        var (status, title, detail, errors) = ex switch
        {
            ValidationException v => (400, "Validation failed", "One or more validation errors occurred.",
               v.Errors.GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray())),

            UnauthorizedException u => (401, "Unauthorized", u.Message, null),
            NotFoundException n => (404, "Not found", n.Message, null),
            ConflictException c => (409, "Conflict", c.Message, null),
            DomainValidationException d => (400, "Validation failed", d.Message, null),
            _ => (500, "Server error", "An unexpected error occurred.", null)
        };

        if (status == 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path
        };
        if (errors is not null) problem.Extensions["errors"] = errors;
        problem.Extensions["traceId"] = ctx.TraceIdentifier;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}