namespace MergeService.Models
{
    public class SyncRecord
    {
        public string? Email { get; set; }
        public string? Status { get; set; }
        public Dictionary<string, Report>? Fields {get; set; }
        public string? NewerSource { get; set; }
    }

    public class Report
    {
        public string? SystemAValue { get; set; }
        public string? SystemBValue { get; set; }
        public string? Status {get; set; } 
    }
}