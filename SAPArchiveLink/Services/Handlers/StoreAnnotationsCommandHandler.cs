

namespace SAPArchiveLink
{
    public class StoreAnnotationsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.STOREANNOTATIONS;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Annotations stored");
        }
    }
}
