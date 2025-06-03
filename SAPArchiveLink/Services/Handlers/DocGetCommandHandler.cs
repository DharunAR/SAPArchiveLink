

using System.Text;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class DocGetCommandHandler : ICommandHandler
    {
        private ICMArchieveLinkClient _archiveClient;
        private ICommandResponseFactory _responseFactory;

        public DocGetCommandHandler(ICMArchieveLinkClient archiveClient, ICommandResponseFactory responseFactory)
        {
            _archiveClient = archiveClient;
            _responseFactory = responseFactory;
        }

        /// <summary>
        /// Command Template
        /// </summary>
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGET;

        /// <summary>
        /// Handles the SAP ArchiveLink 'docGet' command
        /// Retrieves either a single document component (if 'compId' is provided)
        /// or all components using multipart/form-data.
        /// Response includes all required ArchiveLink headers and binary content
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                string docId = command.GetValue(ALParameter.VarDocId);
                string contRep = command.GetValue(ALParameter.VarContRep);
                string compId = command.GetValue(ALParameter.VarCompId);
                string pVersion = command.GetValue(ALParameter.VarPVersion) ?? "0047";
                string secKey = command.GetValue(ALParameter.VarSecKey);
                string accessMode = command.GetValue(ALParameter.VarAccessMode);
                string authId = command.GetValue(ALParameter.VarAuthId);
                string expiration = command.GetValue(ALParameter.VarExpiration);

                // Validate required parameters
                if (string.IsNullOrEmpty(docId) || string.IsNullOrEmpty(contRep))
                    return _responseFactory.CreateError("Missing required parameters: docId and contRep", "ICS_4001");

                if (!string.IsNullOrEmpty(secKey))
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
                    var record = _archiveClient.GetRecord(db, docId, contRep);
                    if (record == null)
                        return _responseFactory.CreateError("Record not found", "ICS_4040", StatusCodes.Status404NotFound);

                    var components = record.ChildSapComponents;

                    // Handle single component response
                    if (!string.IsNullOrEmpty(compId))
                    {
                        if (!_archiveClient.IsRecordComponentAvailable(components, compId))
                            return _responseFactory.CreateError($"Component '{compId}' not found", "ICS_4042", StatusCodes.Status404NotFound);

                        var component = await _archiveClient.GetDocumentComponent(components, compId);
                        var response = _responseFactory.CreateDocumentContent(component.Data, component.ContentType, StatusCodes.Status200OK, component.FileName);

                        response.AddHeader("X-compId", component.CompId);
                        response.AddHeader("X-Content-Length", component.ContentLength.ToString());
                        response.AddHeader("X-compDateC", component.CreationDate.ToUniversalTime().ToString("yyyy-MM-dd"));
                        response.AddHeader("X-compTimeC", component.CreationDate.ToUniversalTime().ToString("HH:mm:ss"));
                        response.AddHeader("X-compDateM", component.ModifiedDate.ToUniversalTime().ToString("yyyy-MM-dd"));
                        response.AddHeader("X-compTimeM", component.ModifiedDate.ToUniversalTime().ToString("HH:mm:ss"));
                        response.AddHeader("X-compStatus", component.Status);
                        response.AddHeader("X-pVersion", component.PVersion ?? pVersion);
                        response.AddHeader("X-docId", docId);
                        response.AddHeader("X-contRep", contRep);

                        return response;
                    }

                    // Handle multipart response (multiple components)
                    var multipartComponents = await _archiveClient.GetDocumentComponents(components);
                    var multipartResponse = _responseFactory.CreateMultipartDocument(multipartComponents);

                    multipartResponse.AddHeader("X-dateC", record.DateCreated.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd"));
                    multipartResponse.AddHeader("X-timeC", record.DateCreated.ToDateTime().ToUniversalTime().ToString("HH:mm:ss"));
                    multipartResponse.AddHeader("X-dateM", record.DateModified.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd"));
                    multipartResponse.AddHeader("X-timeM", record.DateModified.ToDateTime().ToUniversalTime().ToString("HH:mm:ss"));
                    multipartResponse.AddHeader("X-contRep", contRep);
                    multipartResponse.AddHeader("X-numComps", components.Count.ToString());
                    multipartResponse.AddHeader("X-docId", docId);
                    multipartResponse.AddHeader("X-docStatus", "online");
                    multipartResponse.AddHeader("X-pVersion", pVersion);

                    return multipartResponse;
                }       
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError($"Internal server error: {ex.Message}", "ICS_5000", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
