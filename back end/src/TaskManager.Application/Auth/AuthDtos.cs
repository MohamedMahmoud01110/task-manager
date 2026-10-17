using FluentValidation;
using TaskManager.Domain.Enums;
using static TaskManager.Domain.Enums.Enums;

namespace TaskManager.Application.Auth;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record CreateUserRequest(string Name, string Email, string Password, UserRole Role);

public record UserDto(Guid Id, string Name, string Email, UserRole Role, DateTime CreatedAt);
public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);

public static class Rules
{
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().MaximumLength(254).EmailAddress().WithMessage("Email format is invalid.");

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().MinimumLength(8).MaximumLength(100)
         .Matches("[A-Za-z]").WithMessage("Password must contain a letter.")
         .Matches("[0-9]").WithMessage("Password must contain a digit.");
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).ValidPassword();
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.Role).IsInEnum();
    }
}