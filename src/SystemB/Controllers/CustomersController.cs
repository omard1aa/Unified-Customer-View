using Microsoft.AspNetCore.Mvc;
using SystemB.Helpers;
using SystemB.Services;
namespace SystemB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomersService _service;

        public CustomersController(ICustomersService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{email}")]
        public async Task<IActionResult> GetCustomersByEmail(string email)
        {
            var data = await _service.GetByEmailAsync(email);
            if (data is null) return NotFound("Customer not found.");
            return Ok(data);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var results = await _service.SearchAsync(query);
            return Ok(results);
        }

        [HttpGet("health")]
        public async Task<IActionResult> CheckHealth()
        {
            var healthy = await _service.IsHealthyAsync();
            return healthy ? Ok(new { status = "healthy" }) : StatusCode(503, new { status = "unhealthy" });
        }
    }
}