﻿

namespace SAPArchiveLink
{
    public class CreatePostCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.CREATEPOST;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            string contRep = command.GetValue("contRep");
            if (string.IsNullOrEmpty(contRep))
            {
                return CommandResponse.ForProtocolText("contRep is required for CREATE",400);
            }
            return CommandResponse.ForProtocolText($"Document created (POST) in repository {contRep}");
        }
    }
}
