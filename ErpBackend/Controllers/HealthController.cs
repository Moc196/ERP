using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                // Kiểm tra xem có kết nối được DB không
                var canConnect = _context.Database.CanConnect();
                return Ok(new { 
                    Status = "Healthy", 
                    Database = canConnect ? "Connected" : "Disconnected",
                    Time = DateTime.Now 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
    }
}
