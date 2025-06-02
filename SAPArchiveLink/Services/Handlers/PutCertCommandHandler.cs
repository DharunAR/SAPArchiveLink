

using Microsoft.Extensions.Logging;
using System.Net;

namespace SAPArchiveLink
{
    public class PutCertCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.PUTCERT;
        private readonly ILogHelper<PutCertCommandHandler> _logger;

        public PutCertCommandHandler(ILogHelper<PutCertCommandHandler> helperLogger)
        {
            _logger = helperLogger;
        }
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            const string MN = "PutCert";
            _logger.LogInformation($"{MN} - Start processing");
            // Logger.Enter(MN); // Assuming Logger is your logging class

            //  IAccessIdentifier accessIdentifier = CreateAccessIdentifier(command, context);
            string contRep = command.GetValue(ALParameter.VarContRep);
            string authId = command.GetValue(ALParameter.VarAuthId);
            string permissions = command.GetValue(ALParameter.VarPermissions);

          //  Stream? inputStream = null;

            try
            {
                using (var inputStream = context.GetInputStream())
                {
                    using (var outputStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[2048];
                        int bytesRead;

                        while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputStream.Write(buffer, 0, bytesRead);
                        }

                       // ICSArchiveIdentifier archive = ICSObjectFactory.CreateArchiveIdentifier(contRep);
                       // _basicService.PutCert(accessIdentifier, authId, outputStream.ToArray(), archive, permissions);
                    }
                }
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
            return CommandResponse.ForProtocolText("Certificate published");

           // return new CommandResponse("Certificate updated");
        }
    }
}
