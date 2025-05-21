

namespace SAPArchiveLink
{
    public class MCreateCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.MCREATE;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.FromText("Multiple documents created");
        }
    }
}
