namespace TaskFlow.Application.Users;

public sealed record UserDto(Guid Id, string DisplayName, string Email, string Color);
