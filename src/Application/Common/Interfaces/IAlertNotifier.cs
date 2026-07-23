using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// Application-layer abstraction over "push this alert to whoever is watching this board".
/// Implemented with SignalR in Infrastructure so Application has zero knowledge of transport.
/// </summary>
public interface IAlertNotifier
{
    Task NotifyBoardAsync(Alert alert, CancellationToken cancellationToken = default);
}
