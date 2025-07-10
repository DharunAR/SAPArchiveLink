namespace SAPArchiveLink
{
    public class SapDocumentComponentModel
    {
        public string DocId { get; set; }
        public string CompId { get; set; }
        public string ContentType { get; set; }
        public string Charset { get; set; }
        public string Version { get; set; }
        public long ContentLength { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Status { get; set; }
        public string PVersion { get; set; }
        public Stream Data { get; set; }
        public string FileName { get; set; }      
    }
}
