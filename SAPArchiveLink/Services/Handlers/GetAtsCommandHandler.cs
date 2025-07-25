

using System.Diagnostics.CodeAnalysis;

namespace SAPArchiveLink
{
    [ExcludeFromCodeCoverage]
    public class GetAtsCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.GETATS;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("ATS retrieved");
        }
    }
}
