using System;
using System.Reflection;
using TRIM.SDK;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{
    private readonly ILogHelper<BaseServices> _logger;
    private readonly IDatabaseConnection _databaseConnection;
    private ICommandResponseFactory _responseFactory;
    private IDownloadFileHandler _downloadFileHandler;
    const string COMP_DATA = "data";
    const string COMP_DATA1 = "data1";

    public BaseServices(ILogHelper<BaseServices> helperLogger, ICommandResponseFactory commandResponseFactory, IDatabaseConnection databaseConnection, IDownloadFileHandler downloadFileHandler)
    {
        _logger = helperLogger;
        _responseFactory = commandResponseFactory;
        _databaseConnection = databaseConnection;
        _downloadFileHandler = downloadFileHandler;
    }

    /// <summary>
    /// Handles the SAP ArchiveLink 'putCert' command.
    /// </summary>
    /// <param name="authId"></param>
    /// <param name="inputStream"></param>
    /// <param name="contRepId"></param>
    /// <param name="permissions"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> PutCert(string authId, Stream inputStream, string contRepId, string permissions)
    {
        try
        {
            const string MN = "PutCert";
            if (string.IsNullOrWhiteSpace(authId))
            {
                _logger.LogError($"{MN} - Missing required parameter: authId");
                //need to look at the error code here,  it is a 404 error
                return _responseFactory.CreateError("Missing required parameter: authId", StatusCodes.Status404NotFound);
            }
            if (string.IsNullOrWhiteSpace(contRepId))
            {
                _logger.LogError($"{MN} - Missing required parameter: contRep");
                return _responseFactory.CreateError("\"Parameter 'contRep' must not be null or empty", StatusCodes.Status404NotFound);
            }

            using var memoryStream = new MemoryStream();
            byte[] buffer = new byte[1024 * 2]; // 2 KB buffer
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, bytesRead);
            }

            int protectionLevel = -1;
            if (!string.IsNullOrWhiteSpace(permissions))
            {
                protectionLevel = SecurityUtils.AccessModeToInt(permissions);
            }

            //await _archiveClient.PutArchiveCertificate(authId, protectionLevel, memoryStream.ToArray(), contRepId);

            return _responseFactory.CreateProtocolText("Certificate published");
        }
        catch (Exception)
        {
            throw;
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
                return _responseFactory.CreateError("Record not found", StatusCodes.Status404NotFound);

            // Handle single component response
            if (!string.IsNullOrWhiteSpace(sapDoc.CompId))
            {
                if (!recordAdapter.HasComponent(sapDoc.CompId))
                    return _responseFactory.CreateError($"Component '{sapDoc.CompId}' not found", StatusCodes.Status404NotFound);

                var component = await recordAdapter.ExtractComponentById(sapDoc.CompId);

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
        try
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
                    return _responseFactory.CreateError("Record not found", StatusCodes.Status404NotFound);
                }

                var components = record.GetAllComponents();

                var compId = GetComponentId(sapDoc.CompId, record);

                if (string.IsNullOrEmpty(compId))
                {
                    return _responseFactory.CreateError("No valid component found", StatusCodes.Status404NotFound);
                }

                if (!record.HasComponent(compId))
                {
                    return _responseFactory.CreateError($"Component '{compId}' not found", StatusCodes.Status404NotFound);
                }

                var component = await record.ExtractComponentById(compId);
                if (component == null)
                {
                    return _responseFactory.CreateError("Component could not be loaded", StatusCodes.Status500InternalServerError);
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
        catch (Exception ex)
        {
            return _responseFactory.CreateError(ex.Message, StatusCodes.Status500InternalServerError);
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
        SapDocumentComponent[] components = [];
        try
        {
            var validationResults = ModelValidator.Validate(model);

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
                    archiveRecord = trimRepo.CreateRecord(model);
                    if (archiveRecord == null)
                        return _responseFactory.CreateError("Failed to create archive record.");
                }

                // Handle single/multiple components
                if (model.Components != null)
                {
                    components = isMultipart ? model.Components.ToArray() : new[] { model.Components.First() };
                    foreach (SapDocumentComponent comp in components)
                    {
                        if (string.IsNullOrWhiteSpace(comp.CompId))
                            return _responseFactory.CreateError("Component ID is missing.", StatusCodes.Status400BadRequest);

                        if (archiveRecord.HasComponent(comp.CompId))
                            return _responseFactory.CreateError($"A component with ID '{comp.CompId}' already exists in document '{comp.CompId}'.", StatusCodes.Status400BadRequest);

                        var filePath = await _downloadFileHandler.DownloadDocument(comp.Data, comp.FileName);
                        if (string.IsNullOrWhiteSpace(filePath))
                            return _responseFactory.CreateError("Failed to save component file.", StatusCodes.Status400BadRequest);

                        archiveRecord.AddComponent(comp.CompId, filePath, comp.ContentType, comp.Charset, comp.PVersion);
                    }
                }
                archiveRecord.Save();
            }
        }
        finally
        {
            CleanUpFiles(components);
        }
        
        return _responseFactory.CreateProtocolText("Component(s) created successfully.", StatusCodes.Status201Created);
    }

    #region Helper methods

    /// <summary>
    /// Cleans up temporary files created during the document processing.
    /// </summary>
    /// <param name="components"></param>
    private void CleanUpFiles(SapDocumentComponent[] components)
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
    private ICommandResponse GetSingleComponentResponse(SapDocumentComponent component, SapDocumentRequest sapDoc)
    {
        var response = _responseFactory.CreateDocumentContent(component.Data, component.ContentType, StatusCodes.Status200OK, component.FileName);

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

    /// <summary>
    /// Returns Multipart response for DocGet
    /// </summary>
    /// <param name="multipartComponents"></param>
    /// <param name="record"></param>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    private ICommandResponse GetMultiPartResponse(List<SapDocumentComponent> multipartComponents, IArchiveRecord record, SapDocumentRequest sapDoc)
    {
        var multipartResponse = _responseFactory.CreateMultipartDocument(multipartComponents);

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

    #endregion

}
