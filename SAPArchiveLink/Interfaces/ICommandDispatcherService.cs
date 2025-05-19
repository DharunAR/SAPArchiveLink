using Microsoft.AspNetCore.Mvc;


namespace SAPArchiveLink
{
    public interface ICommandDispatcherService
    {
        Task<IActionResult> RunRequest(CommandRequest request);
        void RegisterHandler(ICommandHandler handler);
    }
}
