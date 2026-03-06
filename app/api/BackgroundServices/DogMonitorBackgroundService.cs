using Api.Orchestrators;

namespace Api.BackgroundServices;

public sealed class DogMonitorBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<DogMonitorBackgroundService> logger) : BackgroundService
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
            if (centralNow.Hour < 5 || centralNow.Hour >= 20)
            {
                continue;
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<MonitorOrchestrator>();
                await orchestrator.CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during dog monitor check");
            }
        }
    }
}
