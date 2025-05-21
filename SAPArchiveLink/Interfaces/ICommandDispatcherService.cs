using Microsoft.AspNetCore.Mvc;


namespace SAPArchiveLink
{
    public interface ICommandDispatcherService
    {
        Task<IActionResult> RunRequest(CommandRequest request, ContentServerRequestAuthenticator _authenticator);
        void RegisterHandler(ICommandHandler handler);
    }
}
