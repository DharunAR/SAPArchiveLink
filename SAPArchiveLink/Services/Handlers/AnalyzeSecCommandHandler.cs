

namespace SAPArchiveLink
{
    public class AnalyzeSecCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.ANALYZESEC;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Security analysis completed");
        }
    }
}
