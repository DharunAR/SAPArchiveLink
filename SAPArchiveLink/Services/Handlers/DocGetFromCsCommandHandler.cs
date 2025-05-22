

namespace SAPArchiveLink
{
    public class DocGetFromCsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGETFROMCS;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document retrieved from content server");
        }
    }
}
