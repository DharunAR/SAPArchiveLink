namespace SAPArchiveLink
{
    public interface ITrimRepository : IDisposable
    {
        IArchiveRecord? GetRecord(string docId, string contRep);
        IArchiveRecord? CreateRecord(CreateSapDocumentModel model);
    }

}
