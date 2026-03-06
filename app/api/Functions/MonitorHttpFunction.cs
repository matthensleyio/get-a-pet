using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Api.Orchestrators;

namespace Api.Functions;

public sealed class MonitorHttpFunction(
    MonitorOrchestrator monitorOrchestrator,
    ILogger<MonitorHttpFunction> logger)
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    [Function("Monitor")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "monitor")] HttpRequest req,
        CancellationToken ct)
    {
        var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
        if (centralNow.Hour < 5 || centralNow.Hour >= 20)
        {
            return new OkResult();
        }

        try
        {
            await monitorOrchestrator.CheckAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error during dog monitor check");
        }

        return new OkResult();
    }
}
