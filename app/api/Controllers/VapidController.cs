using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/vapid-public-key")]
public sealed class VapidController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public ActionResult<string> GetPublicKey()
    {
        var key = configuration["VAPID_PUBLIC_KEY"];

        if (String.IsNullOrEmpty(key))
        {
            return StatusCode(500, "VAPID public key not configured");
        }

        return Ok(key);
    }
}
