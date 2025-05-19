

namespace SAPArchiveLink
{
    public class GetHeadCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GET_HEAD;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Get HEAD operation completed");
        }
    }
}
