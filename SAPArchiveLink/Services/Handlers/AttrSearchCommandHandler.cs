using SAPArchiveLink.Resources;

namespace SAPArchiveLink
{
    public class AttrSearchCommandHandler : ICommandHandler
    {
        const string COMP_DESCR = "descr";
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.ATTRSEARCH;

        private readonly ILogHelper<AttrSearchCommandHandler> _logger;
        private readonly ICommandResponseFactory _responseFactory;
        private readonly IBaseServices _baseServices;

        public AttrSearchCommandHandler(
            ILogHelper<AttrSearchCommandHandler> logger,
            ICommandResponseFactory responseFactory,
            IBaseServices baseServices)
        {
            _logger = logger;
            _responseFactory = responseFactory;
            _baseServices = baseServices;
        }

        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                var request = new SapSearchRequestModel
                {
                    ContRep = command.GetValue(ALParameter.VarContRep),
                    DocId = command.GetValue(ALParameter.VarDocId),
                    CompId = command.GetValue(ALParameter.VarCompId) ?? COMP_DESCR,
                    Pattern = command.GetValue(ALParameter.VarPattern),
                    PVersion = command.GetValue(ALParameter.VarPVersion),
                    CaseSensitive = command.GetValue(ALParameter.VarCaseSensitive)?.Trim().ToLowerInvariant() == "y",
                    FromOffset = int.TryParse(command.GetValue(ALParameter.VarFromOffset), out var from) ? from : 0,
                    ToOffset = int.TryParse(command.GetValue(ALParameter.VarToOffset), out var to) ? to : -1,
                    NumResults = int.TryParse(command.GetValue(ALParameter.VarNumResults), out var count) ? count : 1,
                    AccessMode = command.GetValue(ALParameter.VarAccessMode),
                    AuthId = command.GetValue(ALParameter.VarAuthId),
                    Expiration = command.GetValue(ALParameter.VarExpiration),
                    SecKey = command.GetValue(ALParameter.VarSecKey)
                };

                if (request.FromOffset < 0 || request.ToOffset < -1)
                {
                    _logger.LogError("Invalid offset values: FromOffset must be >= 0, ToOffset must be >= -1");
                    return _responseFactory.CreateError(Resource.InvalidOffset, StatusCodes.Status400BadRequest);
                }

                return await _baseServices.GetAttrSearchResult(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception on AttrSearchCommandHandler", ex);
                return _responseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
        }

    }

}
