

namespace SAPArchiveLink
{
    public class DelAttributeCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DELATTRIBUTE;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Attribute deleted");
        }
    }
}
