using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SystemB.Models;
namespace SystemB.Helpers
{
    public static class DataHandling
    {
        public static async Task<List<Customer>> LoadDataFromJson(string filePath = "Data/SystemBData.json")
        {
            if (!File.Exists(filePath))
                return new List<Customer>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Customer>>(json) ?? new List<Customer>();
        }
    }
}