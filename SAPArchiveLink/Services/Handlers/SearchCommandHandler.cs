namespace SAPArchiveLink
{
    public class SearchCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.SEARCH;

        private readonly ILogHelper<SearchCommandHandler> _logger;
        private ICommandResponseFactory _responseFactory;
        IBaseServices _baseServices;

        public SearchCommandHandler(ILogHelper<SearchCommandHandler> helperLogger, ICommandResponseFactory responseFactory, IBaseServices baseServices)
        {
            _logger = helperLogger;
            _responseFactory = responseFactory;
            _baseServices = baseServices;
        }

        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var sapSearchRequest = new SapSearchRequestModel
                {
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    DocId = command.GetValue(ALParameter.VarDocId),
                    Pattern = command.GetValue(ALParameter.VarPattern),
                    CompId = command.GetValue(ALParameter.VarCompId),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    CaseSensitive = command.GetValue(ALParameter.VarCaseSensitive) == "1",
                    FromOffset = command.GetValue(ALParameter.VarFromOffset) != null ? int.Parse(command.GetValue(ALParameter.VarFromOffset)) : 0,
                    ToOffset = command.GetValue(ALParameter.VarToOffset) != null ? int.Parse(command.GetValue(ALParameter.VarToOffset)) : -1,
                    NumResults = command.GetValue(ALParameter.VarNumResults) != null ? int.Parse(command.GetValue(ALParameter.VarNumResults)) : 1,                 
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                    SecKey = command.GetValue(ALParameter.VarSecKey)

                };

                 return await _baseServices.GetSearchResult(sapSearchRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
