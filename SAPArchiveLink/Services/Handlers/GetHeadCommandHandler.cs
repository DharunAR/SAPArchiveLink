

namespace SAPArchiveLink
{
    public class GetHeadCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GET_HEAD;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Get HEAD operation completed");
        }
    }
}
