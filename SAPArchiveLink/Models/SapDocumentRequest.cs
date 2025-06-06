namespace SAPArchiveLink
{
    public class SapDocumentRequest
    {
        public string DocId { get; set; }
        public string ContRep { get; set; }
        public string CompId { get; set; }
        public string PVersion { get; set; }
        public string SecKey { get; set; }
        public string AccessMode { get; set; }
        public string AuthId { get; set; }
        public string Expiration { get; set; }
        public long FromOffset { get; set; }
        public long ToOffset { get; set; }
    }

}
