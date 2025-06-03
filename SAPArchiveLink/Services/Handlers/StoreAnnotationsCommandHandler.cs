

namespace SAPArchiveLink
{
    public class StoreAnnotationsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.STOREANNOTATIONS;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Annotations stored");
        }
    }
}
