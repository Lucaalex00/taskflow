namespace TaskFlow.Infrastructure.Workers;

/// <summary>Bound from configuration section "LoadMonitor" (see appsettings.json).</summary>
public sealed class LoadMonitorOptions
{
    public const string SectionName = "LoadMonitor";

    /// <summary>How often the worker wakes up to snapshot metrics and evaluate rules.</summary>
    public int IntervalSeconds { get; set; } = 60;
}
