namespace MergeService.Interfaces
{
    public interface IDataSourceClient
    {
        Task<T?> GetByEmailAsync<T>(string email) where T : class;
        Task<List<T>> SearchAsync<T>(string query) where T : class;
        Task<bool> IsHealthyAsync();
    }

    public interface ISystemAClient : IDataSourceClient { }
    public interface ISystemBClient : IDataSourceClient { }
}
