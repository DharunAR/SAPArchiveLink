using TRIM.SDK;

namespace SAPArchiveLink
{
    public class RecordSapComponentsAdapter
    {
        private readonly RecordSapComponents _sdkComponents;

        public RecordSapComponentsAdapter(RecordSapComponents sdkComponents)
        {
            _sdkComponents = sdkComponents;
        }

        public List<SapDocumentComponentModel> GetAllComponents()
        {
            var components = new List<SapDocumentComponentModel>();
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                components.Add(MapToSapDocumentComponentModel(sdkComponent));
            }
            return components;
        }

        public SapDocumentComponentModel? GetComponentById(string compId)
        {
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                if (sdkComponent.ComponentId == compId)
                {
                    return MapToSapDocumentComponentModel(sdkComponent);
                }
            }
            return null;
        }

        public IRecordSapComponent? FindComponentById(string compId)
        {
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                if (sdkComponent.ComponentId == compId)
                {
                    return new RecordSapComponentWrapper(sdkComponent);
                }
            }
            return null;
        }

        private SapDocumentComponentModel MapToSapDocumentComponentModel(RecordSapComponent sdkComponent)
        {
            return new SapDocumentComponentModel
            {
                CompId = sdkComponent.ComponentId,
                ContentType = sdkComponent.ContentType,
                Charset = sdkComponent.CharacterSet,
                Version = sdkComponent.ApplicationVersion,
                ContentLength = sdkComponent.Bytes,
                CreationDate = sdkComponent.ArchiveDate,
                ModifiedDate = sdkComponent.DateModified,
                Status = "online",
                PVersion = sdkComponent.ArchiveLinkVersion,
                FileName = sdkComponent.FileName
            };
        }
        public void UpdateComponent(IRecordSapComponent updatedComponent, SapDocumentComponentModel model)
        {
            if (!string.IsNullOrEmpty(model.ContentType))
                updatedComponent.ContentType = model.ContentType;

            if (!string.IsNullOrEmpty(model.Charset))
                updatedComponent.Charset = model.Charset;

            if (!string.IsNullOrEmpty(model.Version))
                updatedComponent.ApplicationVersion = model.Version;

            if (!string.IsNullOrEmpty(model.PVersion))
                updatedComponent.ArchiveLinkVersion = model.PVersion;

            updatedComponent.DateModified = TrimDateTime.Now;

            if (!string.IsNullOrEmpty(model.FileName))
                updatedComponent.SetDocument(model.FileName);                 
        }

        public void AddComponent(string compId, string version, string contentType, string charSet, string filePath)
        {
            var now = TrimDateTime.Now;
            var sapComponent = _sdkComponents.New();

            sapComponent.ComponentId = compId;
            sapComponent.ApplicationVersion = version;
            sapComponent.ContentType = contentType;
            sapComponent.CharacterSet = charSet;
            sapComponent.ArchiveDate = now;
            sapComponent.DateModified = now;
            sapComponent.SetDocument(filePath);
        }
    }
}
