using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class DeleteCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DELETE;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string docId = command.GetValue("docId");
            if (string.IsNullOrEmpty(docId))
            {
                return new CommandResponse("docId is required for DELETE") { StatusCode = 400 };
            }
            return new CommandResponse($"Document {docId} deleted");
        }
    }
}
