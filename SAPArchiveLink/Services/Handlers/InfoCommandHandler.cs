using SAPArchiveLink.Resources;
namespace SAPArchiveLink
{
    public class InfoCommandHandler : ICommandHandler
    {
        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;

        public InfoCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
        }

        /// <summary>
        /// Command Template
        /// </summary>
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.INFO;

        /// <summary>
        /// Handles the SAP ArchiveLink 'info' command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
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
                    ResultAs = command.GetValue(ALParameter.VarResultAs)
                };

                return await _baseService.GetDocumentInfo(sapDocumentRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
        }
    }
}
