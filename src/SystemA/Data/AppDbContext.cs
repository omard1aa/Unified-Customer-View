using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SystemA.Models;

namespace SystemA.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, ILogger<AppDbContext> logger) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Email).IsRequired(false);
            });
        }

        public async Task SeedAsync(string seedFilePath)
        {
            if (await Customers.AnyAsync())
            {
                logger.LogInformation("Database already seeded, skipping.");
                return;
            }

            var json = await File.ReadAllTextAsync(seedFilePath);
            var customers = JsonSerializer.Deserialize<List<Customer>>(json,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            if (customers is null || customers.Count == 0)
            {
                logger.LogWarning("Seed file was empty or could not be parsed: {Path}", seedFilePath);
                return;
            }

            Customers.AddRange(customers);
            await SaveChangesAsync();

            logger.LogInformation($"[SystemA] Database seeded successfully with {customers.Count} customers.");
        }
    }
}
