using FluentValidation;

namespace TaskFlow.Application.Common.Validation;

/// <summary>
/// The single source of truth for what makes a password acceptable, so registration (and any
/// future "change password" flow) enforce identical rules and the frontend can mirror them.
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 10;

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinLength).WithMessage($"Password must be at least {MinLength} characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}
