using Microsoft.AspNetCore.Mvc;
using SystemA.Services;

namespace SystemA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController(ICustomersService service) : ControllerBase
    {
        [HttpGet("{email}")]
        public async Task<IActionResult> GetCustomer(string email)
        {
            var customer = await service.GetByEmailAsync(email);
            if (customer is null) return NotFound();
            return Ok(customer);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var results = await service.SearchAsync(q);
            return Ok(results);
        }

        [HttpGet("health")]
        public async Task<IActionResult> CheckHealth()
        {
            var healthy = await service.IsHealthyAsync();
            return healthy ? Ok(new { status = "healthy" }) : StatusCode(503, new { status = "unhealthy" });
        }
    }
}
