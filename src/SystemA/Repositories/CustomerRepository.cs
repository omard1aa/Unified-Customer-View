using Microsoft.EntityFrameworkCore;
using SystemA.Data;
using SystemA.Models;

namespace SystemA.Repositories
{
    public class CustomerRepository(AppDbContext db) : ICustomerRepository
    {
        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            return await db.Customers.FirstOrDefaultAsync(c => c.email == email);
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string query)
        {
            var lower = query.ToLower();
            return await db.Customers
                .Where(c => (c.email != null && c.email.ToLower().Contains(lower)) ||
                            (c.name != null && c.name.ToLower().Contains(lower)))
                .ToListAsync();
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                return await db.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Database connection failed.", ex);
            }
        }
    }
}
