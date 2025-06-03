

namespace SAPArchiveLink
{
    public class CreatePlaceholderCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPLACEHOLDER;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Placeholder created");
        }
    }
}
