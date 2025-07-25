

using System.Diagnostics.CodeAnalysis;

namespace SAPArchiveLink
{
    [ExcludeFromCodeCoverage]
    public class MigrateCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.MIGRATE;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document migrated");
        }
    }
}
