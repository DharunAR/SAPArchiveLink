

namespace SAPArchiveLink
{
    public class ReinitCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.REINIT;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Reinitialization completed");
        }
    }
}
