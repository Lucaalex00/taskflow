using MediatR;

namespace TaskFlow.Application.Users.Commands.ChangePassword;

/// <summary>Changes the signed-in user's password. Requires the current password even though
/// the request is already authenticated, so a stolen/left-open session can't lock the real
/// owner out of their own account.</summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
