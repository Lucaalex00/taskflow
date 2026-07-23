namespace TaskFlow.Application.Boards;

public sealed record BoardDto(Guid Id, string Name, Guid OwnerId, string Color, int TaskCount, DateTime CreatedAtUtc);
