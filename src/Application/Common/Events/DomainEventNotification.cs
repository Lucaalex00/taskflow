using MediatR;
using TaskFlow.Domain.Common;

namespace TaskFlow.Application.Common.Events;

/// <summary>
/// Wraps a Domain-layer <see cref="IDomainEvent"/> as a MediatR notification, since Domain
/// cannot reference MediatR (see ADR 0002 — Domain has zero external dependencies).
/// Infrastructure publishes one of these per domain event after a successful SaveChanges.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
