

namespace SAPArchiveLink
{
    public class LockCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.LOCK;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.FromText("Document locked");
        }
    }
}
