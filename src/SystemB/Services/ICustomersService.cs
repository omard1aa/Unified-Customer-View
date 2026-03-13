using SystemB.Models;
namespace SystemB.Services
{
    public interface ICustomersService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByEmailAsync(string email);
        Task<IEnumerable<Customer>> SearchAsync(string query);
        Task<bool> IsHealthyAsync();
    }
}