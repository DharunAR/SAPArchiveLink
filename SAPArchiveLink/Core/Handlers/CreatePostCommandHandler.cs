using SAPArchiveLink.Helpers;
using SAPArchiveLink.Services;

namespace SAPArchiveLink.Core.Handlers
{
    public class CreatePostCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATE_POST;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string contRep = command.GetValue("contRep");
            if (string.IsNullOrEmpty(contRep))
            {
                return new CommandResponse("contRep is required for CREATE") { StatusCode = 400 };
            }
            return new CommandResponse($"Document created (POST) in repository {contRep}");
        }
    }
}
