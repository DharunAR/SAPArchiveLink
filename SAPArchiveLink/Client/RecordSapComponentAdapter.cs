using TRIM.SDK;

namespace SAPArchiveLink
{
    /// <summary>
    /// Adapter class for handling SAP components in the Record SDK.
    /// </summary>
    public class RecordSapComponentsAdapter
    {
        private readonly RecordSapComponents _sdkComponents;

        public RecordSapComponentsAdapter(RecordSapComponents sdkComponents)
        {
            _sdkComponents = sdkComponents;
        }

        /// <summary>
        /// Retrieves all components associated with the SAP document.
        /// </summary>
        /// <returns></returns>
        public List<SapDocumentComponentModel> GetAllComponents()
        {
            var components = new List<SapDocumentComponentModel>();
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                components.Add(MapToSapDocumentComponentModel(sdkComponent));
            }
            return components;
        }

        /// <summary>
        /// Retrieves a specific component by its ID.
        /// </summary>
        /// <param name="compId"></param>
        /// <returns></returns>
        public SapDocumentComponentModel? GetComponentById(string compId)
        {
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                if (string.Equals(sdkComponent.ComponentId, compId, StringComparison.OrdinalIgnoreCase))
                {
                    return MapToSapDocumentComponentModel(sdkComponent);
                }
            }
            return null;
        }

        /// <summary>
        /// finds a component by its ID and returns it as an IRecordSapComponent.
        /// </summary>
        /// <param name="compId"></param>
        /// <returns></returns>
        public IRecordSapComponent? FindComponentById(string compId)
        {
            foreach (RecordSapComponent sdkComponent in _sdkComponents)
            {
                if (string.Equals(sdkComponent.ComponentId, compId, StringComparison.OrdinalIgnoreCase))
                {
                    return new RecordSapComponentWrapper(sdkComponent);
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the properties of an existing SAP component based on the provided model.
        /// </summary>
        /// <param name="updatedComponent"></param>
        /// <param name="model"></param>

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

        /// <summary>
        /// Adds a new SAP component to the record with the specified properties.
        /// </summary>
        /// <param name="compId"></param>
        /// <param name="version"></param>
        /// <param name="contentType"></param>
        /// <param name="charSet"></param>
        /// <param name="filePath"></param>
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
    }
}
