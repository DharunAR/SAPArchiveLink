

namespace SAPArchiveLink
{
    public class UpdateCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.UPDATE_PUT; // Also handles UPDATE_POST
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document updated");
        }
    }
}
