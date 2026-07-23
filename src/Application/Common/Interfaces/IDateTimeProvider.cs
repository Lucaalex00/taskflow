namespace TaskFlow.Application.Common.Interfaces;

/// <summary>Abstracts DateTime.UtcNow so time-dependent logic (overdue checks) is unit-testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
