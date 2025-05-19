

namespace SAPArchiveLink
{
    public class SetDocFlagCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.SETDOCFLAG;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Document flag set");
        }
    }
}
