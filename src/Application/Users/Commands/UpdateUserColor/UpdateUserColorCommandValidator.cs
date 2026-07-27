using FluentValidation;

namespace TaskFlow.Application.Users.Commands.UpdateUserColor;

public sealed class UpdateUserColorCommandValidator : AbstractValidator<UpdateUserColorCommand>
{
    public UpdateUserColorCommandValidator()
    {
        // Mirrors ColorPalette.IsValidHex (6-digit hex). The domain re-checks it too, but
        // validating here gives a clean 400 through the standard ValidationBehavior pipeline.
        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("A color is required.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex value like #a855f7.");
    }
}
