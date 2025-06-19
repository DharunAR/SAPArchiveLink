

namespace SAPArchiveLink
{
    public class CreatePostCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPOST;
        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;
        private IDownloadFileHandler _downloadFileHandler;

        public CreatePostCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService, IDownloadFileHandler fileHandleRequest)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
            _downloadFileHandler = fileHandleRequest;
        }
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var request = context.GetRequest();
                string docId = command.GetValue(ALParameter.VarDocId);

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
                    Components = SapDocumentComponentModel,
                    ContentType = request.ContentType,
                };

                return await _baseService.CreateRecord(sapDocumentCreateRequest,true);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
