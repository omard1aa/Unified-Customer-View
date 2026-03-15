using MergeService.Interfaces;

namespace MergeService.Clients
{
    public class SystemBClient(HttpClient httpClient) : BaseClient(httpClient), ISystemBClient
    {
    }
}
