namespace TaskFlow.Application.Boards;

public sealed record BoardDto(Guid Id, string Name, Guid OwnerId, int TaskCount, DateTime CreatedAtUtc);
