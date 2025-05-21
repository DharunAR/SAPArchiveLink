

namespace SAPArchiveLink
{
    public class DocGetCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGET;

        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string docId = command.GetValue("docId");
            if (string.IsNullOrEmpty(docId))
            {
                return CommandResponse.FromText("docId is required for DOCGET",400);
            }
            return CommandResponse.FromText($"Document {docId} retrieved");
        }
    }
}
