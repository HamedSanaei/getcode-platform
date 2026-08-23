namespace GetCode.Worker;

internal sealed class WorkerHeartbeat(ILogger<WorkerHeartbeat> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("worker.started");
        while (!stoppingToken.IsCancellationRequested)
        {
            // Placeholder only. Durable jobs/outbox leasing are implemented by roadmap tasks.
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
