

namespace SAPArchiveLink
{
    public class GetCommandHandler : ICommandHandler
    {
        private readonly IBaseServices _baseService;
        private readonly ICommandResponseFactory _responseFactory;

        public GetCommandHandler(IBaseServices baseService, ICommandResponseFactory responseFactory)
        {
            _baseService = baseService;
            _responseFactory = responseFactory;
        }

        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GET;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                long.TryParse(command.GetValue(ALParameter.VarFromOffset), out long fromOffset);
                long.TryParse(command.GetValue(ALParameter.VarToOffset), out long toOffset);
                var sapDocumentRequest = new SapDocumentRequest
                {
                    DocId = command.GetValue(ALParameter.VarDocId),
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    FromOffset = fromOffset,
                    ToOffset = toOffset
                };

                return await _baseService.GetSapDocument(sapDocumentRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(ex.Message);
            }
        }
    }
}
