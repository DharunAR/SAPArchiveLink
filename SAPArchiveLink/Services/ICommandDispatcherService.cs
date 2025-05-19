using Microsoft.AspNetCore.Mvc;

namespace SAPArchiveLink.Services
{
    public interface ICommandDispatcherService
    {
        Task<IActionResult> RunRequest(CommandRequest request);
        void RegisterHandler(ICommandHandler handler);
    }
}
