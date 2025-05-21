

namespace SAPArchiveLink
{
    public class CsrvSettingsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CSRVSETTINGS;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.FromText("Content server settings retrieved");
        }
    }
}
