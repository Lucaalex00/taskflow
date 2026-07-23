using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// Configurable threshold evaluated periodically by the load-monitoring worker.
/// Kept data-only in the Domain; the actual evaluation logic lives behind
/// IAlertRuleEvaluator implementations (Strategy pattern) in Infrastructure,
/// keyed by RuleType, so adding a new rule type never touches this entity.
/// </summary>
public class AlertRule : Entity
{
    public Guid BoardId { get; private set; }
    public AlertRuleType RuleType { get; private set; }
    public int Threshold { get; private set; }
    public int EvaluationWindowMinutes { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AlertRule() { } // EF Core

    private AlertRule(Guid boardId, AlertRuleType ruleType, int threshold, int evaluationWindowMinutes)
    {
        BoardId = boardId;
        RuleType = ruleType;
        Threshold = threshold;
        EvaluationWindowMinutes = evaluationWindowMinutes;
        IsEnabled = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<AlertRule> Create(
        Guid boardId, AlertRuleType ruleType, int threshold, int evaluationWindowMinutes)
    {
        if (boardId == Guid.Empty)
            return Result.Failure<AlertRule>("A rule must belong to a board.");

        if (threshold <= 0)
            return Result.Failure<AlertRule>("Threshold must be a positive number.");

        if (evaluationWindowMinutes <= 0)
            return Result.Failure<AlertRule>("Evaluation window must be a positive number of minutes.");

        return Result.Success(new AlertRule(boardId, ruleType, threshold, evaluationWindowMinutes));
    }

    public void Disable() => IsEnabled = false;
    public void Enable() => IsEnabled = true;

    public void UpdateThreshold(int newThreshold)
    {
        if (newThreshold > 0)
            Threshold = newThreshold;
    }
}
