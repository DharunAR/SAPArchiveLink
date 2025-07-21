using SAPArchiveLink.Resources;
namespace SAPArchiveLink
{
    public class UpdatePostCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.UPDATE_POST;

        private readonly ICommandResponseFactory _responseFactory;
        private readonly IBaseServices _baseService;
        private readonly IDownloadFileHandler _downloadFileHandler;

        public UpdatePostCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService, IDownloadFileHandler fileHandleRequest)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
            _downloadFileHandler = fileHandleRequest;
        }

        /// <summary>
        /// Handles the SAP ArchiveLink 'updatePost' command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var request = context.GetRequest();
                string docId = command.GetValue(ALParameter.VarDocId);

                // Ensure contentType is not null or empty
                if (string.IsNullOrEmpty(request.ContentType))
                {
                    return _responseFactory.CreateError("Content-Type header is missing or invalid.", StatusCodes.Status400BadRequest);
                }

                List<SapDocumentComponentModel> SapDocumentComponentModel = await _downloadFileHandler.HandleRequestAsync(request.ContentType, request.Body, docId);
                var sapDocumentCreateRequest = new CreateSapDocumentModel
                {
                    DocId = docId,
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    ContentLength = request.ContentLength?.ToString() ?? "0",
                    SecKey = command.GetValue(ALParameter.VarSecKey),
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                    Stream = request.Body,
                    Charset = request.Headers["charset"].ToString(),
                    Version = request.Headers["version"].ToString(),
                    DocProt = request.Headers["docprot"].ToString(),
                    ContentType = request.ContentType,
                    Components = SapDocumentComponentModel
                };
               

                return await _baseService.UpdateRecord(sapDocumentCreateRequest,true);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
        }
    }
}
