namespace SAPArchiveLink
{
    public class AdminContRepCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.ADMINCONTREP;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.FromText("Admin content repository retrieved");
        }
    }
}
