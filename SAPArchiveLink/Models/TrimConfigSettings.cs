namespace SAPArchiveLink
{
    /// <summary>
    /// Trim Configuration Details
    /// </summary>
    public class TrimConfigSettings
    {
        public string? DatabaseId { get; set; }
        public int WorkgroupServerPort { get; set; }
        public string? WorkgroupServerName { get; set; }
        public string? BinariesLoadPath { get; set; }
    }
}
