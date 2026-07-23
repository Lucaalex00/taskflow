using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Behaviors;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Services;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers, the validation pipeline behavior, and every
    /// FluentValidation validator found in this assembly. Call once from the API's Program.cs.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IBoardAuthorizer, BoardAuthorizer>();

        return services;
    }
}
