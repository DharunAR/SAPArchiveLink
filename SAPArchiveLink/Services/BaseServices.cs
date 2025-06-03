using SAPArchiveLink;
using System.Net.Sockets;
using System.Security;

namespace SAPArchiveLink;
public class BaseServices : IBaseServices
{
    private ICommandResponseFactory _responseFactory;
    private ICMArchieveLinkClient _archiveClient;
    public BaseServices(ICMArchieveLinkClient archiveClient, ICommandResponseFactory responseFactory)
    {
        _archiveClient = archiveClient;
        _responseFactory = responseFactory;
    }

    public void PutCert(string authId, byte[] certificate, string contRepId, string permissions)
    {
        const string methodName = "PutCert";     

    // LC.Enter(methodName);

    string errorAuthId = "Parameter 'auth id' must not be null or empty.";
       // string titleAuthId = UALConstants.INVALID_PARAMETER;
        ArchiveCertificate icsArchiveCertificate;

        try
        {
            icsArchiveCertificate = ArchiveCertificate.FromByteArray(certificate);
        }
        catch (Exception ex) // RuntimeException | ICSException in Java
        {
            string certErrorMessage = "Bad Format of certificate. Got error: " + ex.Message;
            string errorTitle = "wrong format of certificate";
            //LC.Warn(methodName, certErrorMessage);
            //throw new ICSJDSException(ICSException.ERROR_DS_GENERIC, UALConstants.INVALID_CERTIFICATE_FORMAT,
            //                          UALConstants.HTTP_NOT_ACCEPTABLE_CODE, errorTitle, certErrorMessage,
            //                          new SecurityException(certErrorMessage));
        }

        string content64 = Convert.ToBase64String(certificate);

        if (string.IsNullOrEmpty(authId))
        {
           // LC.Warn(methodName, "Invalid authId");
            //throw new ICSJDSException(ICSException.INVALID_PARAMETER, UALConstants.INVALID_AUTH_ID,
            //                          UALConstants.HTTP_BAD_REQUEST, titleAuthId, errorAuthId,
            //                          new ArgumentException(errorAuthId));
        }

        int protectionLevel = -1;
        if (!string.IsNullOrEmpty(permissions))
        {
            protectionLevel = CommonUtils.ConvertProtection(permissions);
        }

        string archiveName = contRepId;
        long? archiveDataID = null;

        if (!string.IsNullOrWhiteSpace(archiveName))
        {
           // archiveDataID = GetArchiveIDByName(archiveName);
        }

        try
        {
            //csClient.PutArchiveCertificate(authId, icsArchiveCertificate, protectionLevel, content64, archiveDataID);
            //cmClient.PutArchiveCertificate();
        }
        catch (IOException e)
        {
          //  LC.Warn(methodName, e.Message);
            //throw new ICSException(ICSException.ERROR_WRAPPER, ICSException.ERROR_WRAPPER_STR,
            //                       new object[] { e.GetType().Name, e.Message }, e);
        }

       // LC.Leave(methodName);
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
