using FluentValidation;

namespace TaskFlow.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        // Same rule registration enforces (CreateUserCommandValidator) — a display name that
        // would be rejected at sign-up shouldn't become reachable through the profile form.
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}
