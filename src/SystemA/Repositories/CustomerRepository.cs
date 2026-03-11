using Microsoft.EntityFrameworkCore;
using SystemA.Data;
using SystemA.Models;

namespace SystemA.Repositories
{
    public class CustomerRepository(AppDbContext db) : ICustomerRepository
    {
        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            return await db.Customers.FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string query)
        {
            var lower = query.ToLower();
            return await db.Customers
                .Where(c => (c.Email != null && c.Email.ToLower().Contains(lower)) ||
                            (c.Name != null && c.Name.ToLower().Contains(lower)))
                .ToListAsync();
        }

        public async Task CheckHealthAsync()
        {
            // Implementation for checking health
        }
    }
}
