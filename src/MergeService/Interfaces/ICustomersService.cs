using MergeService.Models;

namespace MergeService.Interfaces
{
    public interface ICustomerService
    {
        Task<UnifiedCustomerRecord?> GetUnifiedCustomerByEmailAsync(string email);
    }
}