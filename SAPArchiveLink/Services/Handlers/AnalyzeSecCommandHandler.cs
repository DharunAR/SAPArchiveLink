

namespace SAPArchiveLink
{
    public class AnalyzeSecCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.ANALYZESEC;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return new CommandResponse("Security analysis completed");
        }
    }
}
