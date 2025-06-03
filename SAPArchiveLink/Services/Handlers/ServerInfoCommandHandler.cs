

namespace SAPArchiveLink
{
    public class ServerInfoCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.SERVERINFO;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Server info retrieved");
        }
    }
}
