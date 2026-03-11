using SystemA.Models;
using SystemA.Repositories;

namespace SystemA.Services
{
    public class CustomersService(ICustomerRepository repository, ILogger<CustomersService> logger) : ICustomersService
    {
        public async Task<Customer?> GetByEmailAsync(string email)
        {
            var customer = await repository.GetCustomerByEmailAsync(email);
            if (customer is null)
                logger.LogWarning("Customer not found for email: {Email}", email);

            return customer;
        }

        public async Task<IEnumerable<Customer>> SearchAsync(string query)
        {
            var results = await repository.SearchCustomersAsync(query);
            logger.LogInformation("Search for '{Query}' returned {Count} result(s).", query, results.Count());
            return results;
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                await repository.CheckHealthAsync();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health check failed.");
                return false;
            }
        }
    }
}
