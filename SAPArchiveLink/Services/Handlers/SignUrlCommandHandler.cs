

namespace SAPArchiveLink
{
    public class SignUrlCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.SIGNURL;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("URL signed");
        }
    }
}
