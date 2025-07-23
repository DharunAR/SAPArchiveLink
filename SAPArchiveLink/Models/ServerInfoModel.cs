namespace SAPArchiveLink
{
    public class ServerInfoModel
    {
        const string status = "Running";
        public string ServerStatus { get; set; } = status;
        public string ServerVendorId { get; set; }
        public string ServerVersion { get; set; }
        public string ServerBuild { get; set; }
        public string ServerTime { get; set; }
        public string ServerDate { get; set; }
        public string? ServerStatusDescription { get; set; } = status;
        public string PVersion { get; set; }
        public List<ContentRepositoryInfoModel> ContentRepositories { get; set; } = new();
    }
}
