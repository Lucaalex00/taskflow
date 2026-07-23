using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// A user's membership in a board, with the role that determines what they can do there
/// (see BoardRole). The user who creates a board is added as its first Owner.
/// </summary>
public class BoardMember : Entity
{
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public BoardRole Role { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    private BoardMember() { } // EF Core

    private BoardMember(Guid boardId, Guid userId, BoardRole role)
    {
        BoardId = boardId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public static Result<BoardMember> Create(Guid boardId, Guid userId, BoardRole role)
    {
        if (boardId == Guid.Empty)
            return Result.Failure<BoardMember>("A valid board is required.");

        if (userId == Guid.Empty)
            return Result.Failure<BoardMember>("A valid user is required.");

        return Result.Success(new BoardMember(boardId, userId, role));
    }

    public void ChangeRole(BoardRole newRole)
    {
        Role = newRole;
    }
}
