using System;

namespace SAPArchiveLink
{
    public class PutCertCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.PUTCERT;
        private readonly ILogHelper<PutCertCommandHandler> _logger;
        private ICommandResponseFactory _commandResponseFactory;
        IBaseServices _baseServices;

        public PutCertCommandHandler(ILogHelper<PutCertCommandHandler> helperLogger,ICommandResponseFactory commandResponseFactory,IBaseServices baseServices)
        {
            _logger = helperLogger;          
            _commandResponseFactory = commandResponseFactory;
            _baseServices = baseServices;
        }

        /// <summary>
        /// Handles the SAP ArchiveLink 'putCert' command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            const string MN = "PutCert";
            _logger.LogInformation($"{MN} - Start processing");
            ICommandResponse respose;
            string contRep = command.GetValue(ALParameter.VarContRep);
            string authId = command.GetValue(ALParameter.VarAuthId);
            string permissions = command.GetValue(ALParameter.VarPermissions);
            string pVersion = command.GetValue(ALParameter.VarPVersion);
            var putCertificateModel = new PutCertificateModel
            {
                ContRep = contRep,
                AuthId = authId,
                PVersion = pVersion,
                Permissions = permissions,
                Stream = context.GetInputStream(),
            }; 

            try
            {
                respose = await _baseServices.PutCert(putCertificateModel);       
            }
            catch (Exception ex)
            {
                return _commandResponseFactory.CreateError(ex.Message);
            }
            return respose;
        }
    }
}
