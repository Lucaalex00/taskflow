using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.UnitTests.Common;

public sealed class FakeCurrentUserService(Guid userId) : ICurrentUserService
{
    public Guid UserId { get; } = userId;
}
