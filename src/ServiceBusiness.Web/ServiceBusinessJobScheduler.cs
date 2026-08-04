using Microsoft.Extensions.Options;
using ServiceBusiness.Application;

namespace ServiceBusiness.Web;

public sealed class ServiceBusinessJobScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<JobSchedulerOptions> options,
    ILogger<ServiceBusinessJobScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("ServiceBusiness job scheduler is disabled.");
            return;
        }

        var initialDelay = TimeSpan.FromSeconds(Math.Max(0, options.Value.InitialDelaySeconds));
        if (initialDelay > TimeSpan.Zero)
        {
            await Task.Delay(initialDelay, stoppingToken);
        }

        await RunJobsAsync(stoppingToken);

        using var timer = new PeriodicTimer(GetInterval());
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunJobsAsync(stoppingToken);
        }
    }

    private async Task RunJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<ScheduledJobRunner>();
            var result = await runner.RunOnceAsync(stoppingToken);
            logger.LogInformation(
                "ServiceBusiness scheduled jobs completed. Invoices created: {InvoicesCreated}; emails processed: {EmailsProcessed}.",
                result.InvoicesCreated,
                result.EmailsProcessed);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ServiceBusiness scheduled jobs failed.");
        }
    }

    private TimeSpan GetInterval() =>
        TimeSpan.FromMinutes(Math.Max(1, options.Value.IntervalMinutes));
}
