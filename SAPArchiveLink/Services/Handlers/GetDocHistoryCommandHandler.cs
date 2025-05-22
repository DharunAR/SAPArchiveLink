

namespace SAPArchiveLink
{
    public class GetDocHistoryCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GETDOCHISTORY;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document history retrieved");
        }
    }
}
