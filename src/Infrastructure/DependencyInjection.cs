using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Alerts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Realtime;
using TaskFlow.Infrastructure.Services;
using TaskFlow.Infrastructure.Workers;
using TaskFlow.Infrastructure.Workers.AlertEvaluators;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily from DI (not captured from `configuration` here) so this reflects
        // whatever IConfiguration ends up registered for the running host — critically,
        // including overrides WebApplicationFactory adds in integration tests. Capturing a
        // plain string from `configuration` at this point would silently freeze on whatever
        // ConnectionStrings:Postgres was BEFORE those test-only overrides are layered in,
        // since ConfigureWebHost's ConfigureAppConfiguration only affects the final merged
        // configuration, not this `configuration` reference itself.
        services.AddDbContext<TaskFlowDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Postgres' configuration value.");
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<ITaskFlowDbContext>(sp => sp.GetRequiredService<TaskFlowDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSignalR();
        services.AddScoped<IAlertNotifier, SignalRAlertNotifier>();

        // Strategy pattern: one evaluator per AlertRuleType, resolved as a collection by the worker.
        services.AddScoped<IAlertRuleEvaluator, OverdueTasksThresholdEvaluator>();
        services.AddScoped<IAlertRuleEvaluator, BoardLoadSpikeEvaluator>();
        services.AddScoped<IAlertRuleEvaluator, ConcurrentInProgressThresholdEvaluator>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DemoWorkspaceResetter>();
        services.AddHostedService<DemoResetWorker>();
        services.AddSingleton<IDemoAccountProvider, DemoAccountProvider>();

        services.Configure<LoadMonitorOptions>(configuration.GetSection(LoadMonitorOptions.SectionName));
        services.AddHostedService<LoadMonitorWorker>();

        return services;
    }
}
