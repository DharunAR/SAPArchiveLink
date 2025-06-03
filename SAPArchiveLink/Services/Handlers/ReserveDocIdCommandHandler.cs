

namespace SAPArchiveLink
{
    public class ReserveDocIdCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.RESERVEDOCID;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document ID reserved");
        }
    }
}
