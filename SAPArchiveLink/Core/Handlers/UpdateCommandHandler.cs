using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class UpdateCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.UPDATE_PUT; // Also handles UPDATE_POST
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Document updated");
        }
    }
}
