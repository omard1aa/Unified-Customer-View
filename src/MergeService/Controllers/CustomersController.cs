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
    }
}
