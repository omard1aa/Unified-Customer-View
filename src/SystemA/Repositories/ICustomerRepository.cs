using SystemA.Models;

namespace SystemA.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetCustomerByEmailAsync(string email);
        Task<IEnumerable<Customer>> SearchCustomersAsync(string query);
        Task CheckHealthAsync();
    }
}
