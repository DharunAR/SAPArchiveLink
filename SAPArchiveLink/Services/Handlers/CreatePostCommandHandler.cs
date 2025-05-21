

namespace SAPArchiveLink
{
    public class CreatePostCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATE_POST;
        public async Task<CommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string contRep = command.GetValue("contRep");
            if (string.IsNullOrEmpty(contRep))
            {
                return CommandResponse.FromText("contRep is required for CREATE",400);
            }
            return CommandResponse.FromText($"Document created (POST) in repository {contRep}");
        }
    }
}
