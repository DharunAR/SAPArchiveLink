using SAPArchiveLink.Resources;

namespace SAPArchiveLink
{
    public class AppendCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.APPEND;
        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;      

        public AppendCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;           
        }
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var request = context.GetRequest();
                string docId = command.GetValue(ALParameter.VarDocId);

                var sapDocumentCreateRequest = new AppendSapDocCompModel
                {
                    DocId = docId,
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    SecKey = command.GetValue(ALParameter.VarSecKey),
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                    StreamData = request.Body,
                    ScanPerformed = command.GetValue(ALParameter.VarScanned),
                };

              return  await _baseService.AppendDocument(sapDocumentCreateRequest);

            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
            
        }
    }
}
