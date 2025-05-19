

namespace SAPArchiveLink
{
    public class DistributeContentCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DISTRIBUTECONTENT;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Content distributed");
        }
    }
}
