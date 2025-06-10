using TRIM.SDK;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{   
    private readonly ILogHelper<BaseServices> _logger;
    private ICMArchieveLinkClient _archiveClient;
    private ICommandResponseFactory _responseFactory;
    const string COMP_DATA = "data";
    const string COMP_DATA1 = "data1";

    public BaseServices(ILogHelper<BaseServices> helperLogger, ICMArchieveLinkClient cmArchieveLinkClient, ICommandResponseFactory commandResponseFactory)
    {       
        _archiveClient = cmArchieveLinkClient;
        _logger = helperLogger;
        _responseFactory = commandResponseFactory;
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

            await _archiveClient.PutArchiveCertificate(authId, protectionLevel, memoryStream.ToArray(), contRepId);

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
        if (!string.IsNullOrWhiteSpace(sapDoc.SecKey))
        {
            if (string.IsNullOrWhiteSpace(sapDoc.AccessMode) || string.IsNullOrWhiteSpace(sapDoc.AuthId) || string.IsNullOrWhiteSpace(sapDoc.Expiration))
                return _responseFactory.CreateError("Missing security parameters for signed URL");

            if (!sapDoc.AccessMode.Contains("r"))
                return _responseFactory.CreateError("Read access mode required", StatusCodes.Status401Unauthorized);

            //TODO to implement verification part
            ValidateSignature(sapDoc);
        }

        // Connect to database and retrieve record
        using (var db = _archiveClient.GetDatabase())
        {
            var record = _archiveClient.GetRecord(db, sapDoc.DocId, sapDoc.ContRep);
            if (record == null)
                return _responseFactory.CreateError("Record not found", StatusCodes.Status404NotFound);

            var components = record.ChildSapComponents;

            // Handle single component response
            if (!string.IsNullOrWhiteSpace(sapDoc.CompId))
            {
                if (!_archiveClient.IsRecordComponentAvailable(components, sapDoc.CompId))
                    return _responseFactory.CreateError($"Component '{sapDoc.CompId}' not found", StatusCodes.Status404NotFound);

                var component = await _archiveClient.GetDocumentComponent(components, sapDoc.CompId);

                return GetSingleComponentResponse(component, sapDoc);
            }

            // Handle multipart response (multiple components)
            var multipartComponents = await _archiveClient.GetDocumentComponents(components);
            return GetMultiPartResponse(multipartComponents, record, sapDoc);
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
            if (!string.IsNullOrWhiteSpace(sapDoc.SecKey))
            {
                if (string.IsNullOrWhiteSpace(sapDoc.AccessMode) || string.IsNullOrWhiteSpace(sapDoc.AuthId) || string.IsNullOrWhiteSpace(sapDoc.Expiration))
                {
                    return _responseFactory.CreateError("Missing security parameters for signed URL");
                }               
                if (!sapDoc.AccessMode.Contains("r"))
                {
                    return _responseFactory.CreateError("Read access mode required", StatusCodes.Status401Unauthorized);
                }        
                ValidateSignature(sapDoc);
            }

            using (var db = _archiveClient.GetDatabase())
            {
                var record = _archiveClient.GetRecord(db, sapDoc.DocId, sapDoc.ContRep);
                if (record == null)
                {
                    return _responseFactory.CreateError("Record not found", StatusCodes.Status404NotFound);
                }

                var components = record.ChildSapComponents;

                var compId = GetComponentId(sapDoc.CompId, components);

                if (string.IsNullOrEmpty(compId))
                {
                    return _responseFactory.CreateError("No valid component found", StatusCodes.Status404NotFound);
                }    

                if (!_archiveClient.IsRecordComponentAvailable(components, compId))
                {
                    return _responseFactory.CreateError($"Component '{compId}' not found", StatusCodes.Status404NotFound);
                }
                   
                var component = await _archiveClient.GetDocumentComponent(components, compId);
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


    #region Helper methods

    /// <summary>
    /// Returns single component response for DocGet
    /// </summary>
    /// <param name="component"></param>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    private ICommandResponse GetSingleComponentResponse(SAPDocumentComponent component, SapDocumentRequest sapDoc)
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
    private ICommandResponse GetMultiPartResponse(List<SAPDocumentComponent> multipartComponents, Record record, SapDocumentRequest sapDoc)
    {
        var multipartResponse = _responseFactory.CreateMultipartDocument(multipartComponents);

        multipartResponse.AddHeader("X-dateC", record.DateCreated.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd"));
        multipartResponse.AddHeader("X-timeC", record.DateCreated.ToDateTime().ToUniversalTime().ToString("HH:mm:ss"));
        multipartResponse.AddHeader("X-dateM", record.DateModified.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd"));
        multipartResponse.AddHeader("X-timeM", record.DateModified.ToDateTime().ToUniversalTime().ToString("HH:mm:ss"));
        multipartResponse.AddHeader("X-contRep", sapDoc.ContRep);
        multipartResponse.AddHeader("X-numComps", $"{record.ChildSapComponents.Count}");
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
    /// <param name="components"></param>
    /// <returns></returns>
    private string GetComponentId(string compId, RecordSapComponents components)
    {
        if (!string.IsNullOrWhiteSpace(compId))
            return compId;

        if (_archiveClient.IsRecordComponentAvailable(components, COMP_DATA))
            return COMP_DATA;
        if (_archiveClient.IsRecordComponentAvailable(components, COMP_DATA1))
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
    private async Task<(Stream Stream, long Length, ICommandResponse Error)> GetRangeStream(Stream originalStream, long contentLength, long fromOffset, long toOffset)
    {
        if (fromOffset < 0 || toOffset < 0)
            return (null, 0, _responseFactory.CreateError("Offsets cannot be negative"));

        if (fromOffset >= contentLength)
            return (null, 0, _responseFactory.CreateError("fromOffset is beyond component length"));

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

    public async Task<ICommandResponse> CreateRecord(CreateSapDocumentModel createSapDocumentModels)
    {
        var validationResults = ModelValidator.Validate(createSapDocumentModels);


        if (validationResults.Any())
        {
            foreach (var result in validationResults)
            {
                Console.WriteLine($"Validation Error: {result.ErrorMessage}");
                var errorMessage = result?.ErrorMessage ?? "Unknown validation error";
                return _responseFactory.CreateError(errorMessage, 400);
            }
        }

        return _responseFactory.CreateProtocolText("Record Created");

    }

    #endregion

}
