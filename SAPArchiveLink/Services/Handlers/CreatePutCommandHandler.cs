using SAPArchiveLink.Resources;

namespace SAPArchiveLink
{
    public class CreatePutCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPUT; // Also handles CREATE_POST

        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;
        private IDownloadFileHandler _downloadFileHandler;

        public CreatePutCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService, IDownloadFileHandler fileHandleRequest)
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

                // Ensure contentType is not null or empty
                if (string.IsNullOrEmpty(request.ContentType))
                {
                    return _responseFactory.CreateError(Resource.ContentTypeMissing, StatusCodes.Status400BadRequest);
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
					DocProt = command.GetValue(ALParameter.VarDocProt),
					ContentType = request.ContentType,
                };
                if (SapDocumentComponentModel != null)
                {
                    SapDocumentComponentModel.First().CompId = sapDocumentCreateRequest.CompId;
                    SapDocumentComponentModel.First().Charset = sapDocumentCreateRequest.Charset;
                    sapDocumentCreateRequest.Components = SapDocumentComponentModel;
                }

                return await _baseService.CreateRecord(sapDocumentCreateRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
        }
      
    }

}
