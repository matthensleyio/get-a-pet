using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Api.Orchestrators;

namespace Api.Functions;

public sealed class MonitorTimerFunction(
    MonitorOrchestrator monitorOrchestrator,
    ILogger<MonitorTimerFunction> logger)
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    [Function("MonitorTimer")]
    public async Task RunAsync(
        [TimerTrigger("0 * * * * *")] TimerInfo myTimer,
        CancellationToken ct)
    {
        var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);

        if (centralNow.Hour < 5 || centralNow.Hour >= 20)
        {
            return;
        }

        try
        {
            await monitorOrchestrator.CheckAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error during dog monitor check");
        }
    }
}
