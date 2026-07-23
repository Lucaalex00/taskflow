namespace TaskFlow.Application.Common.Interfaces;

/// <summary>Centralizes the "is the current user allowed to do this on this board" checks so
/// every board-scoped handler enforces membership/ownership the same way.</summary>
public interface IBoardAuthorizer
{
    /// <summary>Throws ForbiddenException unless the current user is a member (Owner or Member) of the board.</summary>
    Task EnsureMemberAsync(Guid boardId, CancellationToken cancellationToken);

    /// <summary>Throws ForbiddenException unless the current user is the Owner of the board.</summary>
    Task EnsureOwnerAsync(Guid boardId, CancellationToken cancellationToken);
}
