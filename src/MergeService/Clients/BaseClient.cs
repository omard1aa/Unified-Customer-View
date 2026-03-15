using System.Net.Http.Json;
using MergeService.Models;

namespace MergeService.Clients
{
    public abstract class BaseClient(HttpClient httpClient)
    {
        protected readonly HttpClient Http = httpClient;

        public async Task<T?> GetByEmailAsync<T>(string email) where T : class
        {
            var response = await Http.GetAsync($"api/customers/{Uri.EscapeDataString(email)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<List<T>> SearchAsync<T>(string query) where T : class
        {
            return await Http.GetFromJsonAsync<List<T>>(
                $"api/customers/search?query={Uri.EscapeDataString(query)}") ?? [];
        }

        public async Task<bool> IsHealthyAsync()
        {
            var response = await Http.GetAsync("api/customers/health");
            return response.IsSuccessStatusCode;
        }
    }
}
