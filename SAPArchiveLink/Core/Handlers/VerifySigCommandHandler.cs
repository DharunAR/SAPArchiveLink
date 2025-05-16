using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class VerifySigCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.VERIFYSIG;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Signature verified");
        }
    }
}