using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.UnitTests.Common;

/// <summary>Permissive stand-in for IBoardAuthorizer, for handler tests whose focus isn't
/// authorization itself. BoardAuthorizerTests exercises the real implementation.</summary>
public sealed class FakeBoardAuthorizer : IBoardAuthorizer
{
    public Task EnsureMemberAsync(Guid boardId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task EnsureOwnerAsync(Guid boardId, CancellationToken cancellationToken) => Task.CompletedTask;
}
