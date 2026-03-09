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
        var force = req.Query["force"] == "true";
        if (!force && (centralNow.Hour < 5 || centralNow.Hour >= 20))
        {
            return new BadRequestObjectResult($"Monitor is outside active hours. Current Central time: {centralNow:HH:mm}. Active window: 05:00–20:00.");
        }

        try
        {
            await monitorOrchestrator.CheckAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error during dog monitor check");

            return new ObjectResult(new ProblemDetails
            {
                Title = "Monitor check failed",
                Detail = ex.ToString(),
                Status = StatusCodes.Status500InternalServerError
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        return new OkObjectResult("Monitor check completed successfully.");
    }
}
