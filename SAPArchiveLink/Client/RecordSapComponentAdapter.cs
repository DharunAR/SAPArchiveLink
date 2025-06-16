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

        public List<SapDocumentComponent> GetAllComponents()
        {
            var components = new List<SapDocumentComponent>();
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                components.Add(MapToSapDocumentComponent(sdkComponent));
            }
            return components;
        }

        public SapDocumentComponent? GetComponentById(string compId)
        {
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                if (sdkComponent.ComponentId == compId)
                {
                    return MapToSapDocumentComponent(sdkComponent);
                }
            }
            return null;
        }

        private SapDocumentComponent MapToSapDocumentComponent(RecordSapComponent sdkComponent)
        {
            return new SapDocumentComponent
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
    }
}
