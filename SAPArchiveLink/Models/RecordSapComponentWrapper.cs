using TRIM.SDK;

namespace SAPArchiveLink
{
    public class RecordSapComponentWrapper : IRecordSapComponent
    {
        private readonly RecordSapComponent _recordSapComponent;

        public RecordSapComponentWrapper(RecordSapComponent recordSapComponent)
        {
            _recordSapComponent = recordSapComponent;
        }

        public string ComponentId
        {
            get => _recordSapComponent.ComponentId;
            set => _recordSapComponent.ComponentId = value;
        }

        public string ContentType
        {
            get => _recordSapComponent.ContentType;
            set => _recordSapComponent.ContentType = value;
        }

        public string Charset
        {
            get => _recordSapComponent.CharacterSet;
            set => _recordSapComponent.CharacterSet = value;
        }

        public string ApplicationVersion
        {
            get => _recordSapComponent.ApplicationVersion;
            set => _recordSapComponent.ApplicationVersion = value;
        }
        public string ArchiveLinkVersion
        {
            get => _recordSapComponent.ArchiveLinkVersion;
            set => _recordSapComponent.ArchiveLinkVersion = value;
        }
        
        public DateTime ArchiveDate
        {
            get => _recordSapComponent.ArchiveDate;
            set => _recordSapComponent.ArchiveDate = value;
        }

        public DateTime DateModified
        {
            get => _recordSapComponent.DateModified;
            set => _recordSapComponent.DateModified = value;
        }

        public void SetDocument(string filePath)
        {
            _recordSapComponent.SetDocument(filePath);
        }

        public void DeleteComponent()
        {
            _recordSapComponent.Delete();
        }
    }
}