

namespace SAPArchiveLink
{
    public class DistributeContentCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DISTRIBUTECONTENT;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Content distributed");
        }
    }
}
