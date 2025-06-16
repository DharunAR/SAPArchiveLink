using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface IArchiveRecord
    {
        List<SapDocumentComponent> GetAllComponents();
        SapDocumentComponent? GetComponentById(string compId);
        bool HasComponent(string compId);
        Task<List<SapDocumentComponent>> ExtractAllComponents();
        Task<SapDocumentComponent?> ExtractComponentById(string compId);
        DateTime DateCreated { get; }
        DateTime DateModified { get; }
        int ComponentCount { get; }
        public void Save();
        void AddComponent(string compId, string filePath, string contentType, string charSet, string version);

    }

}
