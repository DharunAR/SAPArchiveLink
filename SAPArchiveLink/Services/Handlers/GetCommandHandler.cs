

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
                var docId = command.GetValue(ALParameter.VarDocId);
                var contRep = command.GetValue(ALParameter.VarContRep);
                if (string.IsNullOrWhiteSpace(docId) || string.IsNullOrWhiteSpace(contRep))
                    return _responseFactory.CreateError("Missing required parameters: docId and contRep");

                long.TryParse(command.GetValue(ALParameter.VarFromOffset), out long fromOffset);
                long.TryParse(command.GetValue(ALParameter.VarToOffset), out long toOffset);
                var sapDocumentRequest = new SapDocumentRequest
                {
                    DocId = docId,
                    ContRep = contRep,
                    CompId = command.GetValue(ALParameter.VarCompId),
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
