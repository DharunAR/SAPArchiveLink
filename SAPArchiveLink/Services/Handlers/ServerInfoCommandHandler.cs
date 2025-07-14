namespace SAPArchiveLink
{
    public class ServerInfoCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.SERVERINFO;

        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;

        public ServerInfoCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
        }

        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            try
            {
                string contRep = command.GetValue(ALParameter.VarContRep);
                string pVersion = command.GetValue(ALParameter.VarPVersion);
                string resultAs = command.GetValue(ALParameter.VarResultAs);

                return await _baseService.GetServerInfo(contRep, pVersion, resultAs);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
