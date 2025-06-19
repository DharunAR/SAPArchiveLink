namespace SAPArchiveLink
{
    public interface IRecordSapComponent
    {
        string ComponentId { get; set; }
        string ContentType { get; set; }
        string Charset { get; set; }
        string ApplicationVersion { get; set; }
        string ArchiveLinkVersion { get; set; }        
        DateTime ArchiveDate { get; set; }
        DateTime DateModified { get; set; }

        void SetDocument(string filePath);
    }
}
