using SystemA.Models;

namespace SystemA.Services
{
    public interface ICustomersService
    {
        Task<Customer?> GetByEmailAsync(string email);
        Task<IEnumerable<Customer>> SearchAsync(string query);
        Task<bool> IsHealthyAsync();
    }
}
