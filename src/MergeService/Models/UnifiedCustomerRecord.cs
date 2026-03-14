namespace MergeService.Models
{
    public class UnifiedCustomerRecord
    {
        public UnifiedCustomerRecord()
        {   
        }
        public UnifiedCustomerRecord(string id)
        {
            Id = id;
        }
        // Common fields between SystemA and SystemB
        public required string Id { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public string? last_updated { get; set; }

        // SystemA specific fields
        public string? SystemAId { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public string? ContractType { get; set; }

        // SystemB specific fields
        public string? SystemBuuid { get; set; }
        public string? phone { get; set; }

        public MetaData? Metadata { get; set; }
    }

    public class MetaData
    {
        public Dictionary<string, string>? sources { get; set; }
        public List<Dictionary<string, string>>? conflicts { get; set; }
        public bool? isPartial { get; set; }
    }
}