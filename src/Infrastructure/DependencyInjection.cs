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
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Postgres' configuration value.");

        services.AddDbContext<TaskFlowDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITaskFlowDbContext>(sp => sp.GetRequiredService<TaskFlowDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddSignalR();
        services.AddScoped<IAlertNotifier, SignalRAlertNotifier>();

        // Strategy pattern: one evaluator per AlertRuleType, resolved as a collection by the worker.
        services.AddScoped<IAlertRuleEvaluator, OverdueTasksThresholdEvaluator>();
        services.AddScoped<IAlertRuleEvaluator, BoardLoadSpikeEvaluator>();
        services.AddScoped<IAlertRuleEvaluator, ConcurrentInProgressThresholdEvaluator>();

        services.Configure<LoadMonitorOptions>(configuration.GetSection(LoadMonitorOptions.SectionName));
        services.AddHostedService<LoadMonitorWorker>();

        return services;
    }
}
