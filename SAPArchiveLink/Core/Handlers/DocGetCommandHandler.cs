using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class DocGetCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.DOCGET;

        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string docId = command.GetValue("docId");
            if (string.IsNullOrEmpty(docId))
            {
                return new CommandResponse("docId is required for DOCGET") { StatusCode = 400 };
            }
            return new CommandResponse($"Document {docId} retrieved");
        }
    }
}
