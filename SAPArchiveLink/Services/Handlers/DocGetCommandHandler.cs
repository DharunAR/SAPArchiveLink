

using System.Text;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public class DocGetCommandHandler : ICommandHandler
    {
        private ICommandResponseFactory _responseFactory;
        private IBaseServices _baseService;

        public DocGetCommandHandler(ICommandResponseFactory responseFactory, IBaseServices baseService)
        {
            _responseFactory = responseFactory;
            _baseService = baseService;
        }

        /// <summary>
        /// Command Template
        /// </summary>
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGET;

        /// <summary>
        /// Handles the SAP ArchiveLink 'docGet' command
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
                };  
                
                return await _baseService.DocGetSapComponents(sapDocumentRequest);
            }
            catch (Exception ex)
            {
                return _responseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
