namespace TaskFlow.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by entities.
/// Dispatched by infrastructure (e.g. after SaveChanges) to decoupled handlers.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
