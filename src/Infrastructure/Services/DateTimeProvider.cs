using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
