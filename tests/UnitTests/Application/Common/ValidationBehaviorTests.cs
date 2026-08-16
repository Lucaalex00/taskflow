using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application;
using TaskFlow.Application.Common.Behaviors;
using Xunit;
using ValidationException = TaskFlow.Application.Common.Exceptions.ValidationException;

namespace TaskFlow.UnitTests.Application.Common;

/// <summary>
/// Guards the validation pipeline itself. The void-command case is the one worth pinning down:
/// MediatR's parameterless <c>IRequest</c> is not an <c>IRequest&lt;Unit&gt;</c>, so a behavior
/// constrained to <c>IRequest&lt;TResponse&gt;</c> can't be closed for it and DI quietly drops
/// it — the command then reaches its handler completely unvalidated, with no error anywhere.
/// </summary>
public class ValidationBehaviorTests
{
    public sealed record VoidCommand(string Name) : IRequest;

    public sealed class VoidCommandValidator : AbstractValidator<VoidCommand>
    {
        public VoidCommandValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    public sealed class VoidCommandHandler : IRequestHandler<VoidCommand>
    {
        public static bool WasCalled { get; set; }

        public Task Handle(VoidCommand request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private static IServiceProvider BuildPipeline()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ValidationBehaviorTests>());
        services.AddValidatorsFromAssemblyContaining<ValidationBehaviorTests>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AVoidCommand_WithInvalidInput_IsRejectedBeforeReachingItsHandler()
    {
        VoidCommandHandler.WasCalled = false;
        var sender = BuildPipeline().GetRequiredService<ISender>();

        var act = async () => await sender.Send(new VoidCommand(""));

        await act.Should().ThrowAsync<ValidationException>();
        VoidCommandHandler.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task AVoidCommand_WithValidInput_ReachesItsHandler()
    {
        VoidCommandHandler.WasCalled = false;
        var sender = BuildPipeline().GetRequiredService<ISender>();

        await sender.Send(new VoidCommand("ok"));

        VoidCommandHandler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public void EveryCommandTypeInTheApplicationAssembly_CanCloseTheValidationBehavior()
    {
        // A constraint failure here wouldn't throw at startup — it would just silently skip
        // validation for the offending request type, which is why it's asserted explicitly.
        var behavior = typeof(ValidationBehavior<,>);
        var requestTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.GetInterfaces().Any(i => i == typeof(IRequest)
                            || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .ToList();

        requestTypes.Should().NotBeEmpty();

        foreach (var requestType in requestTypes)
        {
            var responseType = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                ?.GetGenericArguments()[0] ?? typeof(Unit);

            var close = () => behavior.MakeGenericType(requestType, responseType);
            close.Should().NotThrow($"{requestType.Name} must be able to run through the validation pipeline");
        }
    }
}
