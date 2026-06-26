using Kentos.Infrastructure.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Quartz scheduling wiring.</summary>
public static class SchedulingExtensions
{
    public static IServiceCollection AddKentosScheduling(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("sample-maintenance");
            q.AddJob<SampleMaintenanceJob>(jobKey);
            q.AddTrigger(t => t
                .ForJob(jobKey)
                .WithIdentity("sample-maintenance-trigger")
                .WithSimpleSchedule(s => s.WithIntervalInHours(1).RepeatForever()));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        return services;
    }
}
