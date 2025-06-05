using SAPArchiveLink;
using System.Net.Sockets;
using System.Security;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{   
    private readonly ILogHelper<BaseServices> _logger;
    private ICMArchieveLinkClient _archiveClient;
    private ICommandResponseFactory _responseFactory;

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
    /// Response includes all required ArchiveLink headers and binary content 
    /// </summary>
    /// <param name="sapDoc"></param>
    /// <returns></returns>
    public async Task<ICommandResponse> DoGetSapDocument(SapDocumentRequest sapDoc)
    {
        // Validate required parameters
        if (string.IsNullOrEmpty(sapDoc.DocId) || string.IsNullOrEmpty(sapDoc.ContRep))
            return _responseFactory.CreateError("Missing required parameters: docId and contRep");

        if (!string.IsNullOrEmpty(sapDoc.SecKey))
        {
            // TODO signature verification to be implemented
            /*
            if (string.IsNullOrEmpty(accessMode) || string.IsNullOrEmpty(authId) || string.IsNullOrEmpty(expiration))
                return _responseFactory.CreateError("Missing security parameters for signed URL", "ICS_4002");

            if (!accessMode.Contains("r"))
                return _responseFactory.CreateError("Read access mode required", "ICS_4010", StatusCodes.Status401Unauthorized);

            string signedPayload = $"{contRep}{docId}{(compId ?? "")}{accessMode}{authId}{expiration}";
            bool isValid = _archiveClient.VerifySignature(signedPayload, secKey);
            if (!isValid)
                return _responseFactory.CreateError("Invalid signature", "ICS_4011", StatusCodes.Status401Unauthorized);
            */
        }

        // Connect to database and retrieve record
        using (var db = _archiveClient.GetDatabase())
        {
            var record = _archiveClient.GetRecord(db, sapDoc.DocId, sapDoc.ContRep);
            if (record == null)
                return _responseFactory.CreateError("Record not found", StatusCodes.Status404NotFound);

            var components = record.ChildSapComponents;

            // Handle single component response
            if (!string.IsNullOrEmpty(sapDoc.CompId))
            {
                if (!_archiveClient.IsRecordComponentAvailable(components, sapDoc.CompId))
                    return _responseFactory.CreateError($"Component '{sapDoc.CompId}' not found", StatusCodes.Status404NotFound);

                var component = await _archiveClient.GetDocumentComponent(components, sapDoc.CompId);
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

            // Handle multipart response (multiple components)
            var multipartComponents = await _archiveClient.GetDocumentComponents(components);
            var multipartResponse = _responseFactory.CreateMultipartDocument(multipartComponents);

            multipartResponse.AddHeader("X-dateC", record.DateCreated.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd"));
            multipartResponse.AddHeader("X-timeC", record.DateCreated.ToDateTime().ToUniversalTime().ToString("HH:mm:ss"));
            multipartResponse.AddHeader("X-dateM", record.DateModified.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd"));
            multipartResponse.AddHeader("X-timeM", record.DateModified.ToDateTime().ToUniversalTime().ToString("HH:mm:ss"));
            multipartResponse.AddHeader("X-contRep", sapDoc.ContRep);
            multipartResponse.AddHeader("X-numComps", components.Count.ToString());
            multipartResponse.AddHeader("X-docId", sapDoc.DocId);
            multipartResponse.AddHeader("X-docStatus", "online");
            multipartResponse.AddHeader("X-pVersion", sapDoc.PVersion);

            return multipartResponse;
        }
    }
}
