namespace TaskFlow.Application.Users;

public sealed record AuthResult(Guid UserId, string DisplayName, string Token);
