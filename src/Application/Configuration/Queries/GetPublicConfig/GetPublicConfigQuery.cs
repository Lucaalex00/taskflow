using MediatR;

namespace TaskFlow.Application.Configuration.Queries.GetPublicConfig;

/// <summary>What an anonymous client is allowed to know about how this instance is configured.
/// Today that's just whether a demo account exists to sign in with.</summary>
public sealed record GetPublicConfigQuery : IRequest<PublicConfigDto>;

/// <summary>
/// When <paramref name="DemoAccountAvailable"/> is true the credentials are filled in and the
/// login screen offers a one-click sign-in. Handing out the demo password on an anonymous
/// endpoint is deliberate: it's a throwaway account on a seeded instance whose credentials are
/// printed in the README anyway, and doing it this way means the demo button goes through the
/// ordinary login endpoint instead of a bypass. A deployment without seeding
/// (<c>SEED_DEMO=false</c>, the default outside the Docker demo) returns nulls.
/// </summary>
public sealed record PublicConfigDto(bool DemoAccountAvailable, string? DemoEmail, string? DemoPassword);
