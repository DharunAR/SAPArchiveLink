﻿

namespace SAPArchiveLink
{
    public class UpdatePostCommandHandler : ICommandHandler
    {
        public ALCommandTemplate CommandTemplate => ALCommandTemplate.UPDATE_POST;
        public async Task<ICommandResponse> HandleAsync(ICommand command, ICommandRequestContext context)
        {
            return CommandResponse.ForProtocolText("Document updated (POST)");
        }
    }
}
