namespace SystemA.Models
{
    public class Customer
    {
        public required string Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public string? ContractType { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}