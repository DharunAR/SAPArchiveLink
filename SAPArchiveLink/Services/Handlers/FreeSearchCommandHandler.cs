

namespace SAPArchiveLink
{
    public class FreeSearchCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.FREESEARCH;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Free search completed");
        }
    }
}
