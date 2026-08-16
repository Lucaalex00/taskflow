using FluentValidation;
using MediatR;
using ValidationException = TaskFlow.Application.Common.Exceptions.ValidationException;

namespace TaskFlow.Application.Common.Behaviors;

/// <summary>
/// Runs all registered FluentValidation validators for a request before it reaches
/// its handler. Controllers/endpoints stay free of manual "if (!ModelState.IsValid)" checks.
/// </summary>
/// <remarks>
/// The constraint is deliberately <c>notnull</c> and not <c>IRequest&lt;TResponse&gt;</c>:
/// MediatR's void <c>IRequest</c> does not derive from <c>IRequest&lt;Unit&gt;</c>, so the
/// tighter constraint made this open generic silently unconstructible for commands that
/// return nothing — and DI skips a behavior it can't close, meaning those commands would run
/// with their validators never invoked.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
