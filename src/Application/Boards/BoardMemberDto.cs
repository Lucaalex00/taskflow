using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards;

public sealed record BoardMemberDto(Guid UserId, string DisplayName, string Email, string Color, BoardRole Role);
