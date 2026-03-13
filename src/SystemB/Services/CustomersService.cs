using SystemB.Helpers;
using SystemB.Models;

namespace SystemB.Services
{
    public class CustomersService : ICustomersService
    {
        private readonly ILogger<CustomersService> _logger;

        public CustomersService(ILogger<CustomersService> logger)
        {
            _logger = logger;
        }
        public async Task<List<Customer>> GetAllAsync()
        {
            var data = await DataHandling.LoadDataFromJson();
            return data;
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            var customers = await GetAllAsync();
            return customers.FirstOrDefault
            (c => c.email?.Equals(email, StringComparison.OrdinalIgnoreCase) == true);
        }

        public async Task<IEnumerable<Customer>> SearchAsync(string query)
        {
            var data = await DataHandling.LoadDataFromJson();
            var searchResult = data.Where(c => (c.email != null && c.email?.ToLower().Contains(query.ToLower()) == true) ||
                                              (c.name != null && c.name?.ToLower().Contains(query.ToLower()) == true)).ToList();
            _logger.LogInformation($"Search for '{query}' returned {searchResult.Count()} result(s).");
            return searchResult;
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var data = await GetAllAsync();
                _logger.LogInformation("Health check passed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed.");
                return false;
            }
                    
        }
    }
}