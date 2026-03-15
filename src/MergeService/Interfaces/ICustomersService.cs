using MergeService.Models;

namespace MergeService.Interfaces
{
    public interface ICustomerService
    {
        Task<UnifiedCustomerRecord?> GetUnifiedCustomerByEmailAsync(string email);
        Task<List<UnifiedCustomerRecord>> SearchAsync(string query);
        Task<SyncRecord> Sync(string email);
        Task<(bool SystemA, bool SystemB)> IsHealthyAsync();
    }
}