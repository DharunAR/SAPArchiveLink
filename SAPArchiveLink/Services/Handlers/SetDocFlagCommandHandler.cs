

using System.Diagnostics.CodeAnalysis;

namespace SAPArchiveLink
{
    [ExcludeFromCodeCoverage]
    public class SetDocFlagCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.SETDOCFLAG;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document flag set");
        }
    }
}
