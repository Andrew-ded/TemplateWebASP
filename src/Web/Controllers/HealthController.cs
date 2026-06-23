//#if (useJwt)
using Microsoft.AspNetCore.Authorization;
//#endif
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
//#if (useApiVersioning)
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
//#endif
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
//#if (useJwt)
    [AllowAnonymous]
//#endif
    public IActionResult Check() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
