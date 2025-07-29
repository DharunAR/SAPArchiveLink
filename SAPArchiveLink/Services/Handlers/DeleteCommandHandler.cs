namespace SAPArchiveLink
{
    public class DeleteCommandHandler : ICommandHandler
    {

        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;

        public DeleteCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
        }

        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DELETE;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var sapDocumentRequest = new SapDocumentRequest
                {
                    DocId = command.GetValue(ALParameter.VarDocId),
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    SecKey = command.GetValue(ALParameter.VarSecKey),
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                };

                return await _baseService.DeleteSapDocument(sapDocumentRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
    }
}
