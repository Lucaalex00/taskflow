using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.UnitTests.Common;

public sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = utcNow;
}
