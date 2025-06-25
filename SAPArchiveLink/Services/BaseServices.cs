using System.Net;
using System.Text;
using TRIM.SDK;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{
    private readonly ILogHelper<BaseServices> _logger;
    private readonly IDatabaseConnection _databaseConnection;
    private ICommandResponseFactory _responseFactory;
    private IDownloadFileHandler _downloadFileHandler;
    private ISdkMessageProvider _messageProvider;
    const string COMP_DATA = "data";
    const string COMP_DATA1 = "data1";
    private ICertificateFactory _certificateFactory;

    public BaseServices(ILogHelper<BaseServices> helperLogger, ICommandResponseFactory commandResponseFactory, IDatabaseConnection databaseConnection, 
        IDownloadFileHandler downloadFileHandler, ISdkMessageProvider messageProvider, ICertificateFactory certificateFactory)
    {
        _logger = helperLogger;
        _responseFactory = commandResponseFactory;
        _databaseConnection = databaseConnection;
        _downloadFileHandler = downloadFileHandler;
        _messageProvider = messageProvider;
        _certificateFactory= certificateFactory;
    }

    /// <summary>
    /// Handles the SAP ArchiveLink 'putCert' command.
    /// </summary>
    /// <param name="authId"></param>
    /// <param name="inputStream"></param>
    /// <param name="contRepId"></param>
    /// <param name="permissions"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> PutCert(PutCertificateModel putCertificateModel)
    {
        try
        {
            var validationResults = ModelValidator.Validate(putCertificateModel);
            if (validationResults.Any())
            {
                var allErrorMessages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
                var combinedErrorMessage = string.Join("; ", allErrorMessages);
                return _responseFactory.CreateError(combinedErrorMessage);
            }

            using var memoryStream = new MemoryStream();
            byte[] buffer = new byte[1024 * 2]; // 2 KB buffer  
            int bytesRead;

            while ((bytesRead = await putCertificateModel.Stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, bytesRead);
            }
            if (memoryStream.ToArray() == null || memoryStream.ToArray().Length == 0)
            {
                return _responseFactory.CreateError("Certificate cannot be recognized", StatusCodes.Status406NotAcceptable);
            }
            IArchiveCertificate? archiveCertificate = null;
            archiveCertificate = _certificateFactory.FromByteArray(memoryStream.ToArray());
            int protectionLevel = -1;
            if (!string.IsNullOrWhiteSpace(putCertificateModel.Permissions))
            {
                protectionLevel = SecurityUtils.AccessModeToInt(putCertificateModel.Permissions);
            }
            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                trimRepo.PutArchiveCertificate(putCertificateModel.AuthId, protectionLevel, archiveCertificate, putCertificateModel.ContRep);
            }

            return _responseFactory.CreateProtocolText("Certificate published");
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while processing PutCert."+ ex);
            return _responseFactory.CreateError("Certificate cannot be recognized", StatusCodes.Status406NotAcceptable);
        }      
    }

    /// <summary>
    /// Retrieves either a single document component (if 'compId' is provided)
    /// or all components using multipart/form-data.
    /// </summary>
    /// <param name="sapDoc"></param>
    /// <returns>Response includes all required ArchiveLink headers and binary content</returns>
    public async Task<ICommandResponse> DocGetSapComponents(SapDocumentRequest sapDoc)
    {
        var validationResults = ModelValidator.Validate(sapDoc);
        if (validationResults.Any())
        {
            var allErrorMessages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
            var combinedErrorMessage = string.Join("; ", allErrorMessages);
            return _responseFactory.CreateError(combinedErrorMessage);
        }

        //TODO to implement verification part
        ValidateSignature(sapDoc);

        using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
        {
            IArchiveRecord recordAdapter = trimRepo.GetRecord(sapDoc.DocId, sapDoc.ContRep);
            if (recordAdapter == null)
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] {sapDoc.DocId} ), StatusCodes.Status404NotFound);

            // Handle single component response
            if (!string.IsNullOrWhiteSpace(sapDoc.CompId))
            {
                var component = await recordAdapter.ExtractComponentById(sapDoc.CompId);
                if (component == null)
                    return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { sapDoc.CompId, sapDoc.DocId}), StatusCodes.Status404NotFound);

                return GetSingleComponentResponse(component, sapDoc);
            }

            // Handle multipart response (multiple components)
            var multipartComponents = await recordAdapter.ExtractAllComponents();
            return GetMultiPartResponse(multipartComponents, recordAdapter, sapDoc);
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
        var validationResults = ModelValidator.Validate(sapDoc);
        if (validationResults.Any())
        {
            var allErrorMessages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
            var combinedErrorMessage = string.Join("; ", allErrorMessages);
            return _responseFactory.CreateError(combinedErrorMessage);
        }

        //TODO
        ValidateSignature(sapDoc);
        using (ITrimRepository db = _databaseConnection.GetDatabase())
        {
            var record = db.GetRecord(sapDoc.DocId, sapDoc.ContRep);
            if (record == null)
            {
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { sapDoc.DocId }), StatusCodes.Status404NotFound);
            }
            if (string.IsNullOrWhiteSpace(sapDoc.CompId))
            {
                sapDoc.CompId = GetComponentId(sapDoc.CompId, record);
                if (string.IsNullOrWhiteSpace(sapDoc.CompId))
                {
                    return _responseFactory.CreateError("No valid component found", StatusCodes.Status404NotFound);
                }
            }
            var component = await record.ExtractComponentById(sapDoc.CompId);
            if (component == null)
            {
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { sapDoc.CompId }), StatusCodes.Status404NotFound);
            }

            var (stream, length, rangeError) = await GetRangeStream(component.Data, component.ContentLength, sapDoc.FromOffset, sapDoc.ToOffset);
            if (rangeError != null)
                return rangeError;

            var response = _responseFactory.CreateDocumentContent(stream, component.ContentType, StatusCodes.Status200OK, component.FileName);
            if (!string.IsNullOrWhiteSpace(component.Charset))
                response.ContentType += $"; charset={component.Charset}";
            if (!string.IsNullOrWhiteSpace(component.Version))
                response.ContentType += $"; version={component.Version}";
            response.AddHeader("Content-Length", length.ToString());

            return response;
        }
    }

    /// <summary>
    /// Creates a new SAP document record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="isMultipart"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> CreateRecord(CreateSapDocumentModel model, bool isMultipart = false)
    {
        SapDocumentComponentModel[] components = [];
        try
        {
            var validationResults = ModelValidator.Validate(model, !isMultipart);

            if (validationResults.Any())
            {
                var combinedMessage = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                return _responseFactory.CreateError(combinedMessage);
            }

            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                // Get existing record if it exists
                var archiveRecord = trimRepo.GetRecord(model.DocId, model.ContRep);
                if (archiveRecord is null)
                {
                    _logger.LogInformation($"Creating new archive record for DocId: {model.DocId}, ContRep: {model.ContRep}");
                    archiveRecord = trimRepo.CreateRecord(model);
                    if (archiveRecord == null)
                        return _responseFactory.CreateError($"Failed to create archive record in {model.ContRep}.");
                }

                // Handle single/multiple components
                if (model.Components != null)
                {
                    components = isMultipart ? model.Components.ToArray() : new[] { model.Components.First() };
                    foreach (SapDocumentComponentModel comp in components)
                    {
                        if (string.IsNullOrWhiteSpace(comp.CompId))
                            return _responseFactory.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest);

                        if (archiveRecord.HasComponent(comp.CompId))
                        {
                            string errorMessage = _messageProvider.GetMessage(MessageIds.sap_componentExists, new string[] { comp.CompId, model.DocId });
                            _logger.LogError(errorMessage);
                            return _responseFactory.CreateError(errorMessage, StatusCodes.Status403Forbidden);
                        }

                        var filePath = await _downloadFileHandler.DownloadDocument(comp.Data, comp.FileName);
                        if (string.IsNullOrWhiteSpace(filePath))
                            return _responseFactory.CreateError("Failed to save component file.", StatusCodes.Status400BadRequest);

                        archiveRecord.AddComponent(comp.CompId, filePath, comp.ContentType, comp.Charset, comp.PVersion);
                    }
                }
                archiveRecord.SetRecordMetadata();
                archiveRecord.Save();
            }
        }
        finally
        {
            CleanUpFiles(components);
        }
        
        return _responseFactory.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created);
    }

    /// <summary>
    /// Updates an existing SAP document record with new components.
    /// </summary>
    /// <param name="createSapDocumentModels"></param>
    /// <param name="isMultipart"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> UpdateRecord(CreateSapDocumentModel createSapDocumentModels, bool isMultipart = false)
    {
        SapDocumentComponentModel[] components = [];
        try
        {
            var validationResults = ModelValidator.Validate(createSapDocumentModels);

            if (validationResults.Any())
            {
                var combinedMessage = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
                return _responseFactory.CreateError(combinedMessage);
            }
            using (ITrimRepository trimRepo = _databaseConnection.GetDatabase())
            {
                // Get existing record if it exists
                var archiveRecord = trimRepo.GetRecord(createSapDocumentModels.DocId, createSapDocumentModels.ContRep);
                if (archiveRecord is null)
                {
                    return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, new string[] { createSapDocumentModels.DocId }));
                }
                else
                {
                    // Handle single/multiple components
                    if (createSapDocumentModels.Components != null)
                    {
                        components = isMultipart ? createSapDocumentModels.Components.ToArray() : new[] { createSapDocumentModels.Components.First() };
                        foreach (SapDocumentComponentModel model in components)
                        {
                            if (string.IsNullOrWhiteSpace(model.CompId))
                                return _responseFactory.CreateError("Component ID was not specified", StatusCodes.Status400BadRequest);

                            IRecordSapComponent? recComp = archiveRecord.FindComponentById(model.CompId);
                            if (recComp == null)
                            {
                                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, new string[] { model.CompId, createSapDocumentModels.DocId }), StatusCodes.Status404NotFound);
                            }
                            else
                            {
                                var filePath = await _downloadFileHandler.DownloadDocument(model.Data, model.FileName);
                                if (string.IsNullOrWhiteSpace(filePath))
                                    return _responseFactory.CreateError("Failed to save component file.", StatusCodes.Status400BadRequest);

                                archiveRecord.UpdateComponent(recComp, model);
                            }
                        }
                    }
                    archiveRecord.SetRecordMetadata();
                    archiveRecord.Save();
                }
            }
        }

        finally
        {
            CleanUpFiles(components);
        }
        return _responseFactory.CreateProtocolText("Component(s) updated successfully.", StatusCodes.Status200OK);
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

        //TODO
        ValidateSignature(sapDoc);

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
                record.DeleteRecord();
                return _responseFactory.CreateProtocolText($"Document {sapDoc.DocId} and all associated components deleted successfully");
            }
            else
            {
                if (record.DeleteComponent(sapDoc.CompId))
                {
                    record.SetRecordMetadata();
                    record.Save();
                    return _responseFactory.CreateProtocolText($"Component {sapDoc.CompId} deleted successfully");
                }
                return _responseFactory.CreateError($"Component {sapDoc.CompId} not found in document {sapDoc.DocId}", StatusCodes.Status404NotFound);
            }
        }
    }

    /// <summary>
    /// Get Document Info from ArchiveLink repository.
    /// </summary>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> GetDocumentInfo(SapDocumentRequest sapDoc)
    {
        var validationResults = ModelValidator.Validate(sapDoc);
        if (validationResults.Any())
        {
            var message = string.Join("; ", validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            return _responseFactory.CreateError(message);
        }

        ValidateSignature(sapDoc);
        bool isHtml = sapDoc.ResultAs?.Equals("html", StringComparison.OrdinalIgnoreCase) == true;

        using var trimRepo = _databaseConnection.GetDatabase();
        var recordAdapter = trimRepo.GetRecord(sapDoc.DocId, sapDoc.ContRep);
        if (recordAdapter == null)
            return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_documentNotFound, [sapDoc.DocId]), StatusCodes.Status404NotFound);

        if (!string.IsNullOrWhiteSpace(sapDoc.CompId))
        {
            var component = await recordAdapter.ExtractComponentById(sapDoc.CompId, extractContent: false);
            if (component == null)
                return _responseFactory.CreateError(_messageProvider.GetMessage(MessageIds.sap_componentNotFound, [sapDoc.CompId, sapDoc.DocId]), StatusCodes.Status404NotFound);

            if (isHtml)
                return CreateHtmlResponse(sapDoc, [component], recordAdapter);

            return GetSingleComponentResponse(component, sapDoc, true);
        }

        if (isHtml)
        {
            var components = recordAdapter.GetAllComponents();
            return CreateHtmlResponse(sapDoc, components, recordAdapter);
        }

        var multipartComponents = await recordAdapter.ExtractAllComponents(extractContent: false);
        return GetMultiPartResponse(multipartComponents, recordAdapter, sapDoc, true);
    }


    #region Helper methods

    /// <summary>
    /// Cleans up temporary files created during the document processing.
    /// </summary>
    /// <param name="components"></param>
    private void CleanUpFiles(SapDocumentComponentModel[] components)
    {
        foreach (var comp in components)
        {
            if (!string.IsNullOrWhiteSpace(comp.FileName))
            {
                _downloadFileHandler.DeleteFile(comp.FileName);
            }
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
        var response = !isInfo ? _responseFactory.CreateDocumentContent(component.Data, component.ContentType, StatusCodes.Status200OK, component.FileName)
            : _responseFactory.CreateInfoMetadata(new List<SapDocumentComponentModel>() { component });

        AddSingleComponentHeaders(component, sapDoc, response);

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
        var multipartResponse = !isInfo ? _responseFactory.CreateMultipartDocument(multipartComponents) 
                                : _responseFactory.CreateInfoMetadata(multipartComponents);

        AddMultiPartHeaders(multipartComponents, record, sapDoc, multipartResponse);

        return multipartResponse;
    }

    private ICommandResponse AddSingleComponentHeaders(SapDocumentComponentModel component, SapDocumentRequest sapDoc, ICommandResponse response)
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

        return response;
    }

    private ICommandResponse AddMultiPartHeaders(List<SapDocumentComponentModel> multipartComponents, IArchiveRecord record, SapDocumentRequest sapDoc, ICommandResponse multipartResponse)
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

        return multipartResponse;
    }


    private void ValidateSignature(SapDocumentRequest sapReq)
    {
        //TODO to implement verification part
    }

    /// <summary>
    /// Fetch component ID from command request, if available.
    /// Otherwise, defaults to the first available component named "data" or "data1", in that order.
    /// </summary>
    /// <param name="compId"></param>
    /// <param name="record"></param>
    /// <returns></returns>
    private string GetComponentId(string compId, IArchiveRecord record)
    {
        if (!string.IsNullOrWhiteSpace(compId))
            return compId;

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
        if (fromOffset < 0 || toOffset < 0)
            return (null, 0L, _responseFactory.CreateError("Offsets cannot be negative"));

        if (fromOffset >= contentLength)
            return (null, 0L, _responseFactory.CreateError("fromOffset is beyond component length"));

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
            AddMultiPartHeaders(components, record, doc, response);

        return response;
    }

    private string BuildHtmlInfoPage(SapDocumentRequest doc, List<SapDocumentComponentModel> components)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><meta charset=\"utf-8\"><title>Document Info</title></head><body>");
        html.AppendLine("<h1>Document Information</h1>");
        html.AppendLine("<table border='1'><tr><th>Field</th><th>Value</th></tr>");
        html.AppendLine($"<tr><td>Document ID</td><td>{HtmlEncode(doc.DocId)}</td></tr>");
        html.AppendLine($"<tr><td>Components</td><td>{components.Count}</td></tr>");
        html.AppendLine("</table>");

        html.AppendLine("<h2>Components</h2>");
        html.AppendLine("<table border='1'><tr><th>ID</th><th>Type</th><th>Length</th><th>Status</th><th>Created</th><th>Modified</th></tr>");

        foreach (var c in components)
        {
            html.AppendLine("<tr>" +
                $"<td>{HtmlEncode(c.CompId)}</td>" +
                $"<td>{HtmlEncode(c.ContentType)}</td>" +
                $"<td>{c.ContentLength}</td>" +
                $"<td>{HtmlEncode(c.Status)}</td>" +
                $"<td>{c.CreationDate:yyyy-MM-dd HH:mm:ss}</td>" +
                $"<td>{c.ModifiedDate:yyyy-MM-dd HH:mm:ss}</td>" +
                "</tr>");
        }

        html.AppendLine("</table>");
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private string HtmlEncode(string? input)
    {
        return WebUtility.HtmlEncode(input ?? string.Empty);
    }

    #endregion

}
