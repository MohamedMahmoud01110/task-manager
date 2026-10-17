using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManager.Api.Filters;

public sealed class ValidationFilter(IServiceProvider sp) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        // Binding errors: malformed JSON, unknown enum value, bad query-string value..
        if (!ctx.ModelState.IsValid)
        {
            var failures = ctx.ModelState
                .Where(kv => kv.Value!.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e => new ValidationFailure(
                    kv.Key.TrimStart('$', '.'),
                    string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)));
            throw new ValidationException(failures);
        }

        foreach (var arg in ctx.ActionArguments.Values)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (sp.GetService(validatorType) is IValidator validator)
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(arg));
                if (!result.IsValid) throw new ValidationException(result.Errors);
            }
        }
        await next();
    }
}