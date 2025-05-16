using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class UnlockCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.UNLOCK;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Document unlocked");
        }
    }
}
