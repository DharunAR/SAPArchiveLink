

namespace SAPArchiveLink
{
    public class DeleteCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DELETE;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string docId = command.GetValue("docId");
            if (string.IsNullOrEmpty(docId))
            {
                return CommandResponse.ForProtocolText("docId is required for DELETE",400);
            }
            return CommandResponse.ForProtocolText($"Document {docId} deleted");
        }
    }
}
