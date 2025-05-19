

namespace SAPArchiveLink
{
    public class GetDocHistoryCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GETDOCHISTORY;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Document history retrieved");
        }
    }
}
