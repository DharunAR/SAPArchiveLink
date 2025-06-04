

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLog;
using System.Net;
using System.Reflection;

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
        /// <exception cref="ALException"></exception>
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            const string MN = "PutCert";
            _logger.LogInformation($"{MN} - Start processing");
            ICommandResponse respose;
            // Logger.Enter(MN); // Assuming Logger is your logging class

            //  IAccessIdentifier accessIdentifier = CreateAccessIdentifier(command, context);
            string contRep = command.GetValue(ALParameter.VarContRep);
            string authId = command.GetValue(ALParameter.VarAuthId);
            string permissions = command.GetValue(ALParameter.VarPermissions);        

            try
            {

                respose = await _baseServices.PutCert(authId, context.GetInputStream(), contRep, permissions);
               
            }
            catch (Exception e)
            {
                //if (inputStream != null)
                //{
                //    CommonUtils.CloseStream(inputStream, _logger);
                //}            
                throw new ALException(
                    "",
                    "",
                    new object[] { e.GetType().Name, e.Message },
                    e
                );
            }
            return respose;

           // return new CommandResponse("Certificate updated");
        }
    }
}
