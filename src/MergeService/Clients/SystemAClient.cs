using MergeService.Interfaces;

namespace MergeService.Clients
{
    public class SystemAClient(HttpClient httpClient) : BaseClient(httpClient), ISystemAClient
    {
    }
}
