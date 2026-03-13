namespace SystemB.Models
{
    public class Customer
    {
        public required string uuid { get; set; }
        public string? email { get; set; }
        public string? name { get; set; }
        public string? phone { get; set; }
        public string? address { get; set; }
        public DateTime last_updated { get; set; }
    }
}