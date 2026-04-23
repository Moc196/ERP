using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "UP", message = "ERP API is running with Swagger!" });
    }
}
