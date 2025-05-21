

namespace SAPArchiveLink
{
    public class GetHeadCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GET_HEAD;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.FromText("Get HEAD operation completed");
        }
    }
}
