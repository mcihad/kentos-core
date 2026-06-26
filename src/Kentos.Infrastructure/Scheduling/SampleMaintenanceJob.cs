using Microsoft.Extensions.Logging;
using Quartz;

namespace Kentos.Infrastructure.Scheduling;

/// <summary>Example Quartz job demonstrating the scheduling pattern.</summary>
[DisallowConcurrentExecution]
public sealed class SampleMaintenanceJob : IJob
{
    private readonly ILogger<SampleMaintenanceJob> _logger;

    public SampleMaintenanceJob(ILogger<SampleMaintenanceJob> logger) => _logger = logger;

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Maintenance job executed at {Time}.", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
