using FluentValidation;

namespace TaskFlow.Application.AlertRules.Commands.CreateAlertRule;

public sealed class CreateAlertRuleCommandValidator : AbstractValidator<CreateAlertRuleCommand>
{
    public CreateAlertRuleCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.RuleType).IsInEnum();
        RuleFor(x => x.Threshold).GreaterThan(0);
        RuleFor(x => x.EvaluationWindowMinutes).GreaterThan(0);
    }
}
