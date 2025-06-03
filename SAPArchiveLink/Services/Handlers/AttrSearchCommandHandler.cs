

namespace SAPArchiveLink
{
    public class AttrSearchCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.ATTRSEARCH;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Attribute search completed");
        }
    }
}
