

namespace SAPArchiveLink
{
    public class CsrvSettingsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CSRVSETTINGS;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Content server settings retrieved");
        }
    }
}
