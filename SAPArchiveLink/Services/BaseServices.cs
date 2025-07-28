using SAPArchiveLink.Resources;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TRIM.SDK;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{

    #region Properties

    private readonly ILogHelper<BaseServices> _logger;
    private readonly IDatabaseConnection _databaseConnection;
    private readonly ICommandResponseFactory _responseFactory;
    private readonly IDownloadFileHandler _downloadFileHandler;
    private readonly ISdkMessageProvider _messageProvider;
    private readonly ICertificateFactory _certificateFactory; 
    private readonly CounterService _counterService;
    
    const int _counterCount = 1;
    const string COMP_DATA = "data";
    const string COMP_DATA1 = "data1";
    const string HTML_FORMAT = "html";
    #endregion

    public BaseServices(ILogHelper<BaseServices> helperLogger, ICommandResponseFactory commandResponseFactory, IDatabaseConnection databaseConnection,
        IDownloadFileHandler downloadFileHandler, ISdkMessageProvider messageProvider, ICertificateFactory certificateFactory, CounterService counterService)
    {
        _logger = helperLogger;
        _responseFactory = commandResponseFactory;
        _databaseConnection = databaseConnection;
        _downloadFileHandler = downloadFileHandler;
        _messageProvider = messageProvider;
        _certificateFactory = certificateFactory;
        _counterService = counterService;
    }

    /// <summary>
    /// Handles the SAP ArchiveLink 'putCert' command.
    /// </summary>
    /// <param name="authId"></param>
    /// <param name="inputStream"></param>
    /// <param name="contRepId"></param>
    /// <param name="permissions"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> PutCert(PutCertificateModel model)
    {
        try
        {
            var validationResults = ModelValidator.Validate(model);
            if (validationResults.Any())
            {
                string combinedErrorMessage = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                return _responseFactory.CreateError(combinedErrorMessage);
            }

            byte[] certBytes;

            if (model.Stream == null || !model.Stream.CanRead)
            {
                return _responseFactory.CreateError("Certificate stream is null or unreadable", StatusCodes.Status406NotAcceptable);
            }

            using (var memoryStream = new MemoryStream())
            {
                await model.Stream.CopyToAsync(memoryStream);
                certBytes = memoryStream.ToArray();
            }

            if (certBytes.Length == 0)
            {
                return _responseFactory.CreateError(Resource.CertificateCannotBeRecognized, StatusCodes.Status406NotAcceptable);
            }

            IArchiveCertificate? archiveCertificate = null;
            try
            {
                archiveCertificate = _certificateFactory.FromByteArray(certBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format(Resource.FailedToParseCertificate, model.AuthId), ex);
                return _responseFactory.CreateError(ex.Message, StatusCodes.Status406NotAcceptable);
            }

            int protectionLevel = !string.IsNullOrWhiteSpace(model.Permissions)
             ? SecurityUtils.AccessModeToInt(model.Permissions)
             : -1;
            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                trimRepo.SaveCertificate
                    (model.AuthId,
                    protectionLevel,
                    archiveCertificate,
                    model.ContRep);
            }

            return _responseFactory.CreateProtocolText(Resource.CertificatePublished);
        }
        catch (Exception ex)
        {
            _logger.LogError(Resource.ErrorSavingCertificate, ex);
            return _responseFactory.CreateError(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves either a single document component (if 'compId' is provided)
    /// or all components using multipart/form-data.
    /// </summary>
    /// <param name="sapDocumentRequest"></param>
    /// <returns>Response includes all required ArchiveLink headers and binary content</returns>
    public async Task<ICommandResponse> DocGetSapComponents(SapDocumentRequest sapDocumentRequest)
    {
        _logger.LogInformation($"DocGet request for DocId: {sapDocumentRequest.DocId}, ContRep: {sapDocumentRequest.ContRep}");
        var validationResults = ModelValidator.Validate(sapDocumentRequest);
        if (validationResults.Any())
        {
            var allErrorMessages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
            var combinedErrorMessage = string.Join("; ", allErrorMessages);
            _logger.LogError($"DocGet Validation errors: {combinedErrorMessage}");
            return _responseFactory.CreateError(combinedErrorMessage);
        }

        using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
        {
            _logger.LogInformation($"Fetching record for DocId: {sapDocumentRequest.DocId} and ContRep: {sapDocumentRequest.ContRep}");
            IArchiveRecord? recordAdapter = trimRepo.GetRecord(sapDocumentRequest.DocId, sapDocumentRequest.ContRep);
            if (recordAdapter == null)
            {
                string errorMessage = _messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { sapDocumentRequest.DocId });
                _logger.LogError(errorMessage);
                return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
            }
            // Handle single component response
            if (!string.IsNullOrWhiteSpace(sapDocumentRequest.CompId))
            {
                var component = await recordAdapter.ExtractComponentById(sapDocumentRequest.CompId);
                if (component == null)
                {
                    string errorMessage = _messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { sapDocumentRequest.CompId, sapDocumentRequest.DocId });
                    _logger.LogError(errorMessage);
                    return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
                }

                return GetSingleComponentResponse(component, sapDocumentRequest);
            }

            // Handle multipart response (multiple components)
            var multipartComponents = await recordAdapter.ExtractAllComponents();
            return GetMultiPartResponse(multipartComponents, recordAdapter, sapDocumentRequest);
        }
    }

    /// <summary>
    /// Retrieves a document component from an SAP ArchiveLink repository using the provided request parameters.
    /// Validates DocId and ContRep, fetches the component (or range), and returns it with appropriate headers.
    /// </summary>
    /// <param name="sapDoc"></param>
    /// <returns>Returns 200 OK on success, 400 for missing parameters, 404 for missing record/component, or 500 for server errors.</returns>
    public async Task<ICommandResponse> GetSapDocument(SapDocumentRequest sapDoc)
    {
        _logger.LogInformation($"Get request for DocId: {sapDoc.DocId}, ContRep: {sapDoc.ContRep}");
        var validationResults = ModelValidator.Validate(sapDoc);
        if (validationResults.Any())
        {
            var allErrorMessages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
            var combinedErrorMessage = string.Join("; ", allErrorMessages);
            _logger.LogError($"Get Validation errors: {combinedErrorMessage}");
            return _responseFactory.CreateError(combinedErrorMessage);
        }

        using (ITrimRepository db = _databaseConnection.GetDatabase())
        {
            _logger.LogInformation($"Fetching record for DocId: {sapDoc.DocId} and ContRep: {sapDoc.ContRep}");
            bool addNoSniff;
            var record = db.GetRecord(sapDoc.DocId, sapDoc.ContRep);
            if (record == null)
            {
                string errorMessage = _messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { sapDoc.DocId });
                _logger.LogError(errorMessage);
                return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
            }
            if (string.IsNullOrWhiteSpace(sapDoc.CompId))
            {
                string? compId = GetComponentId(record);
                if (string.IsNullOrWhiteSpace(compId))
                {
                    return _responseFactory.CreateError(Resource.NoValidComponentFound, StatusCodes.Status404NotFound);
                }
                sapDoc.CompId = compId;
            }
            var component = await record.ExtractComponentById(sapDoc.CompId);
            if (component == null || component.Data == null)
            {
                string errorMessage = _messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { sapDoc.CompId });
                _logger.LogError(errorMessage);
                return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
            }

            var (stream, length, rangeError) = await GetRangeStream(component.Data, component.ContentLength, sapDoc.FromOffset, sapDoc.ToOffset);
            if (rangeError != null)
                return rangeError;

            var response = _responseFactory.CreateDocumentContent(stream!, component.ContentType, StatusCodes.Status200OK, component.FileName);
            if (!string.IsNullOrWhiteSpace(component.Charset))
                response.ContentType += $"; charset={component.Charset}";
            if (!string.IsNullOrWhiteSpace(component.Version))
                response.ContentType += $"; version={component.Version}";
            response.AddHeader("Content-Length", length.ToString());

            //Handle Content-disposition
            string contentDisposition = SecurityUtils.GetContentDispositionValue(component.FileName, response.ContentType?.Split(';')[0], out addNoSniff);
            response.AddHeader("Content-Disposition", contentDisposition);
            if (addNoSniff)
            {
                response.AddHeader("X-Content-Type-Options", "nosniff");
            }
            _counterService.UpdateCounter(sapDoc.ContRep, CounterType.View, _counterCount);
            return response;
        }
    }

    /// <summary>
    /// Creates a new SAP document record.
    /// </summary>
    /// <param name="createSapDocumentModel"></param>
    /// <param name="isMultipart"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> CreateRecord(CreateSapDocumentModel createSapDocumentModel, bool isMultipart = false)
    {
        SapDocumentComponentModel[] components = [];
        int _count = 0;
        try
        {
            var validationResults = ModelValidator.Validate(createSapDocumentModel, !isMultipart);

            if (validationResults.Any())
            {
                var combinedMessage = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                return _responseFactory.CreateError(combinedMessage);
            }

            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                // Get existing record if it exists
                var archiveRecord = trimRepo.GetRecord(createSapDocumentModel.DocId, createSapDocumentModel.ContRep);
                if (archiveRecord is null)
                {
                    _logger.LogInformation($"Creating new archive record for DocId: {createSapDocumentModel.DocId}, ContRep: {createSapDocumentModel.ContRep}");
                    archiveRecord = trimRepo.CreateRecord(createSapDocumentModel);
                    if (archiveRecord == null)
                        return _responseFactory.CreateError($"Failed to create archive record in {createSapDocumentModel.ContRep}.");
                }

                // Handle single/multiple components
                if (createSapDocumentModel.Components != null)
                {
                    components = isMultipart ? createSapDocumentModel.Components.ToArray() : new[] { createSapDocumentModel.Components[0] };
                    foreach (SapDocumentComponentModel comp in components)
                    {
                        if (string.IsNullOrWhiteSpace(comp.CompId))
                            return _responseFactory.CreateError(Resource.ComponentIdNotSpecified, StatusCodes.Status400BadRequest);

                        if (archiveRecord.HasComponent(comp.CompId))
                        {
                            string errorMessage = _messageProvider.GetMessage(MessageIds.sap_componentExists, new string[] { comp.CompId, createSapDocumentModel.DocId });
                            _logger.LogError(errorMessage);
                            return _responseFactory.CreateError(errorMessage, StatusCodes.Status403Forbidden);
                        }
                        var filePath = await _downloadFileHandler.DownloadDocument(comp.Data, comp.FileName);
                        if (string.IsNullOrWhiteSpace(filePath))
                            return _responseFactory.CreateError(Resource.FailedToSaveComponentFile, StatusCodes.Status400BadRequest);

                        archiveRecord.AddComponent(comp.CompId, filePath, comp.ContentType, comp.Charset, comp.PVersion);
                        _count++;
                    }
                }
                archiveRecord.SetRecordMetadata();
                archiveRecord.Save();
                _counterService.UpdateCounter(createSapDocumentModel.ContRep, CounterType.Create,_count);           
               _logger.LogInformation($"Record created successfully for DocId: {createSapDocumentModel.DocId}, ContRep: {createSapDocumentModel.ContRep} with {_count} components.");
            }
        }
        finally
        {
            CleanUpFiles(components);
        }

        return _responseFactory.CreateProtocolText(Resource.ComponentCreatedSuccessfully, StatusCodes.Status201Created);
    }

    /// <summary>
    /// Updates an existing SAP document record with new components.
    /// </summary>
    /// <param name="createSapDocumentModel"></param>
    /// <param name="isMultipart"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> UpdateRecord(CreateSapDocumentModel createSapDocumentModel, bool isMultipart = false)
    {
        SapDocumentComponentModel[] components = [];
        int _count = 0;
        try
        {
            var validationResults = ModelValidator.Validate(createSapDocumentModel);

            if (validationResults.Any())
            {
                var combinedMessage = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                return _responseFactory.CreateError(combinedMessage);
            }
            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                // Get existing record if it exists
                var archiveRecord = trimRepo.GetRecord(createSapDocumentModel.DocId, createSapDocumentModel.ContRep);
                if (archiveRecord is null)
                {
                    return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { createSapDocumentModel.DocId }));
                }
                else
                {
                    // Handle single/multiple components
                    if (createSapDocumentModel.Components != null)
                    {
                        components = isMultipart ? createSapDocumentModel.Components.ToArray() : new[] { createSapDocumentModel.Components[0] };
                        foreach (SapDocumentComponentModel model in components)
                        {
                            if (string.IsNullOrWhiteSpace(model.CompId))
                                return _responseFactory.CreateError(Resource.ComponentIdNotSpecified, StatusCodes.Status400BadRequest);

                            IRecordSapComponent? recComp = archiveRecord.FindComponentById(model.CompId);
                            if (recComp == null)
                            {
                                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { model.CompId, createSapDocumentModel.DocId }), StatusCodes.Status404NotFound);
                            }
                            else
                            {
                                var filePath = await _downloadFileHandler.DownloadDocument(model.Data, model.FileName);
                                if (string.IsNullOrWhiteSpace(filePath))
                                    return _responseFactory.CreateError(Resource.FailedToSaveComponentFile, StatusCodes.Status400BadRequest);
                                archiveRecord.UpdateComponent(recComp, model);
                            }
                            _count++;
                        }
                    }
                    archiveRecord.SetRecordMetadata();
                    archiveRecord.Save();
                    _counterService.UpdateCounter(createSapDocumentModel.ContRep, CounterType.Update, _count);
                    _logger.LogInformation($"Record updated successfully for DocId: {createSapDocumentModel.DocId}, ContRep: {createSapDocumentModel.ContRep} with {_count} components.");
                }
            }
        }

        finally
        {
            CleanUpFiles(components);
        }
        return _responseFactory.CreateProtocolText(Resource.ComponentUpdatedSuccessfully, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Delete the RecordSapDocument with all components if compId is not provided
    /// specific component will be deleted, if compId is presented in the request query parameters
    /// </summary>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> DeleteSapDocument(SapDocumentRequest sapDoc)
    {
        var validationResults = ModelValidator.Validate(sapDoc);
        if (validationResults.Any())
        {
            var allErrorMessages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
            var combinedErrorMessage = string.Join("; ", allErrorMessages);
            return _responseFactory.CreateError(combinedErrorMessage);
        }

        using (var db = _databaseConnection.GetDatabase())
        {
            var record = db.GetRecord(sapDoc.DocId, sapDoc.ContRep);
            if (record == null)
            {
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { sapDoc.DocId }), StatusCodes.Status404NotFound);
            }
            if (string.IsNullOrWhiteSpace(sapDoc.CompId))
            {
                //Delete document with all components
                _logger.LogInformation(string.Format(Resource.DocumentAndComponentsDeleted, sapDoc.DocId));
                record.DeleteRecord();
                _counterService.UpdateCounter(sapDoc.ContRep, CounterType.Delete, _counterCount);
                return _responseFactory.CreateProtocolText(string.Format(Resource.DocumentAndComponentsDeleted, sapDoc.DocId));
            }
            else
            {
                if (record.DeleteComponent(sapDoc.CompId))
                {
                    record.SetRecordMetadata();
                    record.Save();
                    _counterService.UpdateCounter(sapDoc.ContRep, CounterType.Delete, _counterCount);
                    return _responseFactory.CreateProtocolText(string.Format(Resource.ComponentDeletedSuccessfully, sapDoc.CompId));
                }                
                return _responseFactory.CreateError(string.Format(Resource.ComponentNotFoundInDocument, sapDoc.CompId, sapDoc.DocId), StatusCodes.Status404NotFound);
            }
        }
    }

    /// <summary>
    /// Get Document Info from ArchiveLink repository.
    /// </summary>
    /// <param name="sapDocumentRequest"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> GetDocumentInfo(SapDocumentRequest sapDocumentRequest)
    {
        var validationResults = ModelValidator.Validate(sapDocumentRequest);
        if (validationResults.Any())
        {
            var message = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            return _responseFactory.CreateError(message);
        }

        bool isHtml = sapDocumentRequest.ResultAs?.Equals(HTML_FORMAT, StringComparison.OrdinalIgnoreCase) == true;

        using var trimRepo = _databaseConnection.GetDatabase();
        var recordAdapter = trimRepo.GetRecord(sapDocumentRequest.DocId, sapDocumentRequest.ContRep);
        if (recordAdapter == null)
            return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, [sapDocumentRequest.DocId]), StatusCodes.Status404NotFound);

        if (!string.IsNullOrWhiteSpace(sapDocumentRequest.CompId))
        {
            var component = await recordAdapter.ExtractComponentById(sapDocumentRequest.CompId, extractContent: false);
            if (component == null)
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, [sapDocumentRequest.CompId, sapDocumentRequest.DocId]), StatusCodes.Status404NotFound);

            if (isHtml)
                return CreateHtmlResponse(sapDocumentRequest, [component], recordAdapter);

            return GetSingleComponentResponse(component, sapDocumentRequest, true);
        }

        if (isHtml)
        {
            var components = recordAdapter.GetAllComponents();
            return CreateHtmlResponse(sapDocumentRequest, components, recordAdapter);
        }

        var multipartComponents = await recordAdapter.ExtractAllComponents(extractContent: false);
        return GetMultiPartResponse(multipartComponents, recordAdapter, sapDocumentRequest, true);
    }

    /// <summary>
    /// Searches the content of a specific document component for a given pattern.
    /// </summary>
    /// <param name="sapSearchRequest"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<ICommandResponse> GetSearchResult(SapSearchRequestModel sapSearchRequest)
    {
        _logger.LogInformation($"GetSearchResult called for DocId: {sapSearchRequest.DocId}");
        string searchResult = null;
        var validationResults = ModelValidator.Validate(sapSearchRequest, true);
        if (validationResults.Any())
        {
            var message = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            _logger.LogError($"Validation failed in GetSearchResult: {message}");
            return _responseFactory.CreateError(message);
        }

        using var trimRepo = _databaseConnection.GetDatabase();
        var recordAdapter = trimRepo.GetRecord(sapSearchRequest.DocId, sapSearchRequest.ContRep);
        if (recordAdapter == null)
        {
            _logger.LogError($"Document not found in GetSearchResult. DocId: {sapSearchRequest.DocId}, ContRep: {sapSearchRequest.ContRep}");
            return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, [sapSearchRequest.DocId]), StatusCodes.Status404NotFound);
        }

        if (!string.IsNullOrWhiteSpace(sapSearchRequest.CompId))
        {
            var component = await recordAdapter.ExtractComponentById(sapSearchRequest.CompId, extractContent: true);
            if (component == null)
            {
                _logger.LogError($"Component not found in GetSearchResult. CompId: {sapSearchRequest.CompId}, DocId: {sapSearchRequest.DocId}");
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, [sapSearchRequest.CompId, sapSearchRequest.DocId]), StatusCodes.Status404NotFound);
            }

            var extractor = TextExtractorFactory.GetExtractor(component.ContentType);
            if (extractor == null)
            {
                string error = string.Format(Resource.UnsupportedContentType, component.ContentType);
                _logger.LogError(error);
                throw new NotSupportedException(error);
            }

            try
            {
                searchResult = SearchContent(
                    extractor,
                    component.Data,
                    sapSearchRequest.Pattern,
                    sapSearchRequest.FromOffset,
                    sapSearchRequest.ToOffset,
                    sapSearchRequest.CaseSensitive
                );
                _logger.LogInformation($"Search completed in GetSearchResult. DocId: {sapSearchRequest.DocId}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"Error during search in GetSearchResult. DocId: {sapSearchRequest.DocId}, CompId: {sapSearchRequest.CompId}, {ex}");
                return _responseFactory.CreateError($"{Resource.ErrorDuringSearch}: {ex.Message}");
            }
        }        

        return _responseFactory.CreateProtocolText(searchResult);
    }

    /// <summary>
    /// Retrieves server information and content repository details.
    /// </summary>
    /// <param name="contRep"></param>
    /// <param name="pVersion"></param>
    /// <param name="resultAs"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> GetServerInfo(string contRep, string pVersion, string resultAs)
    {
        using var trimRepo = _databaseConnection.GetDatabase();
        var serverInfo = trimRepo.GetServerInfo(pVersion, contRep);

        if (serverInfo.ContentRepositories.Count == 0)
        {
            string errorMessage = string.IsNullOrWhiteSpace(contRep) ? Resource.NoContRepFound : string.Format(Resource.ContRepNotFound, contRep);
            _logger.LogError(errorMessage);
            return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
        }
        if (resultAs?.Equals(HTML_FORMAT, StringComparison.OrdinalIgnoreCase) == true)
        {
            var html = BuildServerInfoHtmlResponse(serverInfo);
            return _responseFactory.CreateHtmlReport(html);
        }
        else
        {
            var infoResponse = BuildServerInfoResponse(serverInfo);
            return _responseFactory.CreateProtocolText(infoResponse);
        }
    }

    public async Task<ICommandResponse> AppendDocument(AppendSapDocCompModel sapDoc)
    {
        SapDocumentComponentModel[] components = Array.Empty<SapDocumentComponentModel>();
        try
        {
            var validationResults = ModelValidator.Validate(sapDoc);
            if (validationResults.Any())
            {
                var message = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                _logger.LogError($"Validation failed in GetSearchResult: {message}");
                return _responseFactory.CreateError(message);
            }

            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                _logger.LogInformation($"Fetching record for DocId: {sapDoc.DocId} and ContRep: {sapDoc.ContRep}");
                IArchiveRecord? archiveRecord = trimRepo.GetRecord(sapDoc.DocId, sapDoc.ContRep);
                if (archiveRecord == null)
                {
                    string errorMessage = _messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { sapDoc.DocId });
                    _logger.LogError(errorMessage);
                    return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
                }

                if (!string.IsNullOrWhiteSpace(sapDoc.CompId))
                {
                    var component = await archiveRecord.ExtractComponentById(sapDoc.CompId);
                    if (component == null)
                    {
                        string errorMessage = _messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { sapDoc.CompId, sapDoc.DocId });
                        _logger.LogError(errorMessage);
                        return _responseFactory.CreateError(errorMessage, StatusCodes.Status404NotFound);
                    }

                    var appender = DocumentAppenderFactory.GetAppender(Path.GetExtension(component.FileName));
                    if (appender == null)
                    {
                        string error = string.Format(Resource.UnsupportedContentType, component.ContentType);
                        _logger.LogError(error);
                        return _responseFactory.CreateError(error, StatusCodes.Status404NotFound);                     
                    }
                    var data = appender.AppendAsync(component.Data, sapDoc.StreamData);

                    var filePath = await _downloadFileHandler.DownloadDocument(data.Result, component.FileName);
                    components = new SapDocumentComponentModel[]
                    {
                        new SapDocumentComponentModel
                        {
                            FileName = filePath
                        }
                    };
                    if (string.IsNullOrWhiteSpace(filePath))
                        return _responseFactory.CreateError(Resource.FailedToSaveComponentFile, StatusCodes.Status400BadRequest);

                    archiveRecord.UpdateComponent(component.RecordSapComponent, components[0]);
                    archiveRecord.SetRecordMetadata();
                    archiveRecord.Save();                    
                }                
            }
        }
        finally
        {
            CleanUpFiles(components);
        }

        return _responseFactory.CreateProtocolText(Resource.ComponentAppendedSuccessfully, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Performs an attribute search on a specific document component.
    /// </summary>
    /// <param name="searchRequest"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> GetAttrSearchResult(SapSearchRequestModel searchRequest)
    {
        _logger.LogInformation($"Executing attrSearch for DocId: {searchRequest.DocId}");

        var validationResults = ModelValidator.Validate(searchRequest);
        if (validationResults.Any())
        {
            var message = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            return _responseFactory.CreateError(message);
        }

        using var trimRepo = _databaseConnection.GetDatabase();
        var record = trimRepo.GetRecord(searchRequest.DocId, searchRequest.ContRep);
        if (record == null)
        {
            var message = _messageProvider.GetMessage(MessageIds.sap_documentNotFound, new[] { searchRequest.DocId });
            return _responseFactory.CreateError(message, StatusCodes.Status404NotFound);
        }

        var component = await record.ExtractComponentById(searchRequest.CompId, extractContent: true);
        if (component == null)
        {
            var message = _messageProvider.GetMessage(MessageIds.sap_componentNotFound, new[] { searchRequest.CompId, searchRequest.DocId });
            return _responseFactory.CreateError(message, StatusCodes.Status404NotFound);
        }

        if (!FindAttributeMatches(
            component.Data,
            searchRequest.Pattern,
            searchRequest.CaseSensitive,
            searchRequest.NumResults,
            searchRequest.FromOffset,
            searchRequest.ToOffset,
            searchRequest.PVersion,
            out var result,
            out var error,
            out var errorStatus))
        {
            return _responseFactory.CreateError(error ?? "Search failed", errorStatus ?? StatusCodes.Status500InternalServerError);
        }

        return _responseFactory.CreateProtocolText(result);
    }


    #region Helper methods

    /// <summary>
    /// Cleans up temporary files created during the document processing.
    /// </summary>
    /// <param name="components"></param>
    private void CleanUpFiles(SapDocumentComponentModel[] components)
    {
        foreach (var comp in components.Where(comp => !string.IsNullOrWhiteSpace(comp.FileName)))
        {
            _downloadFileHandler.DeleteFile(comp.FileName);
        }
    }

    /// <summary>
    /// Returns single component response for DocGet
    /// </summary>
    /// <param name="component"></param>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    private ICommandResponse GetSingleComponentResponse(SapDocumentComponentModel component, SapDocumentRequest sapDoc, bool isInfo = false)
    {
        _logger.LogInformation(isInfo ? Resource.CreatingInfoMetadataSingleComponent : Resource.CreatingDocumentContentSingleComponent);
        var response = !isInfo ? _responseFactory.CreateDocumentContent(component.Data, component.ContentType, StatusCodes.Status200OK, component.FileName)
            : _responseFactory.CreateInfoMetadata(new List<SapDocumentComponentModel>() { component });

        AddSingleComponentHeaders(component, sapDoc, response);
        _counterService.UpdateCounter(sapDoc.ContRep, CounterType.View, _counterCount);
        return response;
    }

    /// <summary>
    /// Returns Multipart response for DocGet
    /// </summary>
    /// <param name="multipartComponents"></param>
    /// <param name="record"></param>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    private ICommandResponse GetMultiPartResponse(List<SapDocumentComponentModel> multipartComponents, IArchiveRecord record, SapDocumentRequest sapDoc, bool isInfo = false)
    {
        _logger.LogInformation(isInfo ? Resource.CreatingInfoMetadataMultiPart : Resource.CreatingDocumentContentMultiPart);
        var multipartResponse = !isInfo ? _responseFactory.CreateMultipartDocument(multipartComponents)
                                : _responseFactory.CreateInfoMetadata(multipartComponents);

        AddMultiPartHeaders(record, sapDoc, multipartResponse);
        _counterService.UpdateCounter(sapDoc.ContRep, CounterType.View, _counterCount);
        return multipartResponse;
    }

    private void AddSingleComponentHeaders(SapDocumentComponentModel component, SapDocumentRequest sapDoc, ICommandResponse response)
    {
        response.AddHeader("X-compId", component.CompId);
        response.AddHeader("X-Content-Length", component.ContentLength.ToString());
        response.AddHeader("X-compDateC", component.CreationDate.ToUniversalTime().ToString("yyyy-MM-dd"));
        response.AddHeader("X-compTimeC", component.CreationDate.ToUniversalTime().ToString("HH:mm:ss"));
        response.AddHeader("X-compDateM", component.ModifiedDate.ToUniversalTime().ToString("yyyy-MM-dd"));
        response.AddHeader("X-compTimeM", component.ModifiedDate.ToUniversalTime().ToString("HH:mm:ss"));
        response.AddHeader("X-compStatus", component.Status);
        response.AddHeader("X-pVersion", component.PVersion ?? sapDoc.PVersion);
        response.AddHeader("X-docId", sapDoc.DocId);
        response.AddHeader("X-contRep", sapDoc.ContRep);
    }

    private void AddMultiPartHeaders(IArchiveRecord record, SapDocumentRequest sapDoc, ICommandResponse multipartResponse)
    {
        multipartResponse.AddHeader("X-dateC", record.DateCreated.ToUniversalTime().ToString("yyyy-MM-dd"));
        multipartResponse.AddHeader("X-timeC", record.DateCreated.ToUniversalTime().ToString("HH:mm:ss"));
        multipartResponse.AddHeader("X-dateM", record.DateModified.ToUniversalTime().ToString("yyyy-MM-dd"));
        multipartResponse.AddHeader("X-timeM", record.DateModified.ToUniversalTime().ToString("HH:mm:ss"));
        multipartResponse.AddHeader("X-contRep", sapDoc.ContRep);
        multipartResponse.AddHeader("X-numComps", $"{record.ComponentCount}");
        multipartResponse.AddHeader("X-docId", sapDoc.DocId);
        multipartResponse.AddHeader("X-docStatus", "online");
        multipartResponse.AddHeader("X-pVersion", sapDoc.PVersion);
    }

    /// <summary>
    /// Fetch component ID from command request, if available.
    /// Otherwise, defaults to the first available component named "data" or "data1", in that order.
    /// </summary>
    /// <param name="compId"></param>
    /// <param name="record"></param>
    /// <returns></returns>
    private string? GetComponentId(IArchiveRecord record)
    {
        if (record.HasComponent(COMP_DATA))
            return COMP_DATA;

        if (record.HasComponent(COMP_DATA1))
            return COMP_DATA1;

        return null;
    }


    /// <summary>
    /// Retrieves a stream for the specified byte range from the component's data stream.
    /// Returns an error response if the offset values are invalid or out of bounds.
    /// </summary>
    /// <param name="originalStream"></param>
    /// <param name="contentLength"></param>
    /// <param name="fromOffset"></param>
    /// <param name="toOffset"></param>
    /// <returns></returns>
    private async Task<(Stream? Stream, long? Length, ICommandResponse? Error)> GetRangeStream(Stream originalStream, long contentLength, long fromOffset, long toOffset)
    {
        _logger.LogInformation(Resource.GetDocumentStream);
        if (fromOffset < 0 || toOffset < 0)
            return (null, 0L, _responseFactory.CreateError(Resource.OffsetsCannotBeNegative));

        if (fromOffset >= contentLength)
            return (null, 0L, _responseFactory.CreateError(Resource.FromOffsetBeyondComponentLength));

        if (fromOffset < toOffset && toOffset <= contentLength)
        {
            var rangeLength = toOffset - fromOffset;
            originalStream.Position = fromOffset;
            var rangeStream = new MemoryStream();
            byte[] buffer = new byte[4096];
            long bytesRemaining = rangeLength;
            int bytesRead;

            while (bytesRemaining > 0 && (bytesRead = await originalStream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, bytesRemaining))) > 0)
            {
                await rangeStream.WriteAsync(buffer, 0, bytesRead);
                bytesRemaining -= bytesRead;
            }
            rangeStream.Position = 0;
            return (rangeStream, rangeLength, null);
        }

        originalStream.Position = 0;
        return (originalStream, contentLength, null);
    }

    private ICommandResponse CreateHtmlResponse(SapDocumentRequest doc, List<SapDocumentComponentModel> components, IArchiveRecord record)
    {
        var html = BuildHtmlInfoPage(doc, components);
        var response = _responseFactory.CreateHtmlReport(html);

        if (components.Count == 1)
            AddSingleComponentHeaders(components[0], doc, response);
        else
            AddMultiPartHeaders(record, doc, response);
        _counterService.UpdateCounter(doc.ContRep, CounterType.View, _counterCount);
        return response;
    }

    private string BuildHtmlInfoPage(SapDocumentRequest doc, List<SapDocumentComponentModel> components)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine($"<html><head><meta charset=\"utf-8\"><title>{Resource.DocInformation}</title></head><body>");
        html.AppendLine($"<h1>{Resource.DocInformation}</h1>");

        html.AppendLine("<table border='1'>");
        html.AppendLine($"<thead><tr><th>{Resource.Field}</th><th>{Resource.Value}</th></tr></thead>");
        html.AppendLine("<tbody>");
        html.AppendLine($"<tr><td>{Resource.DocumentID}</td><td>{HtmlEncode(doc.DocId)}</td></tr>");
        html.AppendLine($"<tr><td>{Resource.Components}</td><td>{HtmlEncode(components.Count.ToString())}</td></tr>");
        html.AppendLine("</tbody></table>");

        html.AppendLine($"<h2>{Resource.Components}</h2>");
        html.AppendLine("<table border='1'>");
        html.AppendLine("<thead><tr>" +
            "<th>ID</th>" +
            $"<th>{Resource.Type}</th>" +
            $"<th>{Resource.Length}</th>" +
            $"<th>{Resource.Status}</th>" +
            $"<th>{Resource.Created}</th>" +
            $"<th>{Resource.Modified}</th>" +
            "</tr></thead><tbody>");

        foreach (var c in components)
        {
            html.AppendLine("<tr>" +
                $"<td>{HtmlEncode(c.CompId)}</td>" +
                $"<td>{HtmlEncode(c.ContentType)}</td>" +
                $"<td>{HtmlEncode(c.ContentLength.ToString())}</td>" +
                $"<td>{HtmlEncode(c.Status)}</td>" +
                $"<td>{HtmlEncode(c.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"))}</td>" +
                $"<td>{HtmlEncode(c.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"))}</td>" +
                "</tr>");
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private string HtmlEncode(string? input)
    {
        return WebUtility.HtmlEncode(input ?? string.Empty);
    }
    
    private string SearchContent(
        ITextExtractor extractor,
        Stream stream,
        string searchText,
        long fromOffset = 0,
        long toOffset = -1,
        bool caseSensitive = false)
    {
        if (stream == null || !stream.CanRead)
            throw new ArgumentException(Resource.StreamMustBeReadable);

        var content = extractor.ExtractText(stream);

        if (string.IsNullOrEmpty(searchText))
            throw new ArgumentException(Resource.SearchTextMustNotBeEmpty);

        // Normalize offsets
        toOffset = toOffset == -1 || toOffset > content.Length ? content.Length : toOffset;
        fromOffset = Math.Clamp(fromOffset, 0, content.Length);
        toOffset = Math.Clamp(toOffset, 0, content.Length);

        if (fromOffset > toOffset)
        {
            // Reverse search range
            content = content.Substring((int)toOffset, (int)(fromOffset - toOffset));
            content = new string(content.Reverse().ToArray());
        }
        else
        {
            content = content.Substring((int)fromOffset, (int)(toOffset - fromOffset));
        }

        var matchOffsets = new List<long>();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int index = 0;
        while ((index = content.IndexOf(searchText, index, comparison)) != -1)
        {
            long offset = fromOffset < toOffset
                ? fromOffset + index
                : fromOffset - index - searchText.Length;

            matchOffsets.Add(offset);
            index += searchText.Length;
        }

        // Format result
        var result = new StringBuilder();
        result.Append(matchOffsets.Count).Append(';');
        foreach (var offset in matchOffsets)
            result.Append(offset).Append(';');

        return result.ToString();
    }

    private string BuildServerInfoResponse(ServerInfoModel model)
    {
        var sb = new StringBuilder();

        sb.Append($"serverStatus=\"{model.ServerStatus}\";");
        sb.Append($"serverVendorId=\"{model.ServerVendorId}\";");
        sb.Append($"serverVersion=\"{model.ServerVersion}\";");
        sb.Append($"serverBuild=\"{model.ServerBuild}\";");
        sb.Append($"serverTime=\"{model.ServerTime}\";");
        sb.Append($"serverDate=\"{model.ServerDate}\";");
        sb.Append($"serverStatusDescription=\"{model.ServerStatusDescription}\";");
        sb.Append($"pVersion=\"{model.PVersion}\";\r\n");

        foreach (var repo in model.ContentRepositories)
        {
            sb.Append($"contRep=\"{repo.ContRep}\";");
            sb.Append($"contRepDescription=\"{repo.ContRepDescription}\";");
            sb.Append($"contRepStatus=\"{repo.ContRepStatus}\";");
            sb.Append($"contRepStatusDescription=\"{repo.ContRepStatusDescription}\";");
            sb.Append($"pVersion=\"{repo.PVersion}\";\r\n");
        }

        return sb.ToString();
    }

    private string BuildServerInfoHtmlResponse(ServerInfoModel model)
    {
        var html = new StringBuilder();

        html.Append("<html><body>");
        html.Append($"<h1>{Resource.CMArchiveLinkStatus}</h1>");
        html.Append($"<p>Status: {HtmlEncode(model.ServerStatus)}</p>");
        html.Append($"<p>Vendor: {HtmlEncode(model.ServerVendorId)}</p>");
        html.Append($"<p>Version: {HtmlEncode(model.ServerVersion)}</p>");
        html.Append($"<p>Build: {HtmlEncode(model.ServerBuild)}</p>");
        html.Append($"<p>Time: {HtmlEncode(model.ServerTime)}</p>");
        html.Append($"<p>Date: {HtmlEncode(model.ServerDate)}</p>");
        html.Append($"<p>Description: {HtmlEncode(model.ServerStatusDescription)}</p>");
        html.Append($"<h2>{Resource.Repositories}</h2><ul>");

        foreach (var repo in model.ContentRepositories)
        {
            html.Append($"<li>{HtmlEncode(repo.ContRep)} - {HtmlEncode(repo.ContRepDescription)} ({HtmlEncode(repo.ContRepStatus)})</li>");
        }

        html.Append("</ul></body></html>");
        return html.ToString();
    }

    private static readonly Regex Pattern0046 = new(@"(\d+)\+(\d+)\+([^#]+)(?:#|$)", RegexOptions.Compiled);
    private static readonly Regex Pattern0047 = new(@"(\d+)\+(\d+)\+([^_]+)(?:_|$)", RegexOptions.Compiled);

    private bool FindAttributeMatches(
        Stream stream,
        string pattern,
        bool caseSensitive,
        int maxResults,
        int fromOffset,
        int toOffset,
        string pVersion,
        out string result,
        out string? errorMessage,
        out int? errorStatus)
    {
        result = string.Empty;
        errorMessage = null;
        errorStatus = null;

        try
        {
            using var reader = new StreamReader(stream, leaveOpen: false);
            var dainLines = new List<(long Offset, long Length, string Data)>();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                if (parts[2].StartsWith("DAIN", StringComparison.OrdinalIgnoreCase) && long.TryParse(parts[0], out var offset) && long.TryParse(parts[1], out var length))
                {
                    var rawData = parts.Length >= 4 ? parts[3] : parts[2].Substring(4);
                    dainLines.Add((offset, length, rawData));
                }
            }

            if (dainLines.Count == 0)
            {
                result = "0;";
                return true;
            }

            // Determine correct pattern parser
            var decodedPattern = HttpUtility.UrlDecode(pattern).Replace(' ', '+');
            var regex = SecurityUtils.ParseVersion(pVersion) == ALProtocolVersion.OO47 ? Pattern0047 : Pattern0046;
            var matches = regex.Matches(decodedPattern ?? "");

            if (matches.Count == 0)
            {
                errorMessage = "Invalid pattern format";
                errorStatus = StatusCodes.Status400BadRequest;
                return false;
            }

            var searchCriteria = new List<(int Offset, int Length, string Value)>();
            foreach (Match match in matches)
            {
                int offset = int.Parse(match.Groups[1].Value);
                int length = int.Parse(match.Groups[2].Value);
                string value = match.Groups[3].Value;

                searchCriteria.Add((offset, length, value));
            }

            bool isForward = fromOffset <= toOffset || toOffset == -1;
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var resultMatches = new List<(long Offset, long Length)>();

            foreach (var (offset, length, data) in dainLines)
            {
                bool inRange = (fromOffset == -1 && toOffset == 0) || // full backward
                               (fromOffset == 0 && toOffset == -1) || // full forward
                               (isForward && offset >= fromOffset && (toOffset == -1 || offset <= toOffset)) ||
                               (!isForward && offset <= fromOffset && (toOffset == -1 || offset >= toOffset));

                if (!inRange) continue;

                bool matched = true;
                foreach (var (searchOffset, searchLength, expectedValue) in searchCriteria)
                {
                    if (data.Length < searchOffset + searchLength)
                    {
                        matched = false;
                        break;
                    }

                    var actual = data.Substring(searchOffset, searchLength).TrimEnd();
                    if (!actual.Equals(expectedValue, comparison))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    resultMatches.Add((offset, length));
                    if (resultMatches.Count >= maxResults) break;
                }
            }

            var sb = new StringBuilder();
            sb.Append(resultMatches.Count).Append(';');
            foreach (var (off, len) in resultMatches)
                sb.Append(off).Append(';').Append(len).Append(';');

            result = sb.ToString();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Attribute search failed", ex);
            errorMessage = "Internal error during attribute search";
            errorStatus = StatusCodes.Status500InternalServerError;
            return false;
        }
    }

    #endregion

}
