namespace SAPArchiveLink
{
    /// <summary>
    /// Trim Configuration Details
    /// </summary>
    public class TrimConfigSettings
    {
        public string? DatabaseId { get; set; }
        public int WGSPort { get; set; }
        public string? WGSName { get; set; }
        public int WGSAlternatePort { get; set; }
        public string? WGSAlternateName { get; set; }
        public string? BinariesLoadPath { get; set; }
        public string? WorkPath { get; set; }
        public string? TrustedUser { get; set; }
        public string? RecordTypeName { get; set; }
    }
}
