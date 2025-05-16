using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class CreatePlaceholderCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPLACEHOLDER;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Placeholder created");
        }
    }
}
