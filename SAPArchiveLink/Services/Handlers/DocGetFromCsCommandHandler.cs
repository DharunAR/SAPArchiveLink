

namespace SAPArchiveLink
{
    public class DocGetFromCsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGETFROMCS;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.FromText("Document retrieved from content server");
        }
    }
}
