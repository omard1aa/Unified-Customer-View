using Microsoft.AspNetCore.Mvc;
using MergeService.Interfaces;
using MergeService.Models;

namespace MergeService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController(ICustomerService service) : ControllerBase
    {
        [HttpGet("{email}")]
        public async Task<ActionResult<UnifiedCustomerRecord>> GetCustomerByEmail([FromRoute] string email)
        {
            var customer = await service.GetUnifiedCustomerByEmailAsync(email);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var results = await service.SearchAsync(q);
            return Ok(results);
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncRequest request)
        {
            var results = await service.Sync(request.Email);
            return Ok(results);
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            var (systemA, systemB) = await service.IsHealthyAsync();
            return Ok(new { SystemA = systemA, SystemB = systemB });
        }
    }
}
