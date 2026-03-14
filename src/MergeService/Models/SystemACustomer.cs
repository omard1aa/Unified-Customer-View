namespace MergeService.Models
{
    public class SystemACustomer
    {
        public required string Id { get; set; }
        public string? email { get; set; }
        public string? name { get; set; }
        public string? address { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public string? ContractType { get; set; }
        public DateTime? last_updated { get; set; }
    }
}