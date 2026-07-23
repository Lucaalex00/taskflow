namespace TaskFlow.Application.Common.Interfaces;

/// <summary>Application depends on this abstraction, not on ASP.NET Core's ClaimsPrincipal —
/// Infrastructure implements it by reading the JWT claims of the current HTTP request.</summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
}
