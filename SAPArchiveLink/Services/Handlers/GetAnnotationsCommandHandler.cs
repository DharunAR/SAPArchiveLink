

namespace SAPArchiveLink
{
    public class GetAnnotationsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GETANNOTATIONS;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Annotations retrieved");
        }
    }
}
