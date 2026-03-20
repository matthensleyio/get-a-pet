using Core.Orchestrators;

namespace GetAPet.Shelter.Import
{
    public sealed class MonitorWorker(IServiceScopeFactory scopeFactory, ILogger<MonitorWorker> logger) : BackgroundService
    {
        private static readonly TimeZoneInfo CentralTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunCheckAsync(stoppingToken);

            var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCheckAsync(stoppingToken);
            }
        }

        private async Task RunCheckAsync(CancellationToken ct)
        {
            var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
            if (centralNow.Hour < 5 || centralNow.Hour >= 20)
            {
                logger.LogInformation("Skipping check outside active hours ({Hour}:xx Central)", centralNow.Hour);
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<MonitorOrchestrator>();

                logger.LogInformation("Monitor check starting at {Time:HH:mm} Central", centralNow);
                await orchestrator.CheckAsync(ct);
                logger.LogInformation("Monitor check completed at {Time:HH:mm} Central", TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monitor check failed");
            }
        }
    }
}
