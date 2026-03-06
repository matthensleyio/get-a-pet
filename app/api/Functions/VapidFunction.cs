using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Api.Functions;

public sealed class VapidFunction(IConfiguration configuration)
{
    [Function("VapidPublicKey")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vapid-public-key")] HttpRequest req)
    {
        var key = configuration["VAPID_PUBLIC_KEY"];

        if (String.IsNullOrEmpty(key))
        {
            return new StatusCodeResult(500);
        }

        return new OkObjectResult(key);
    }
}
