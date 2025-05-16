using Microsoft.AspNetCore.Mvc;
using SAPArchiveLink.Helpers;

namespace SAPArchiveLink.Services
{
    public interface ICommandDispatcherService
    {
        Task<IActionResult> RunRequest(CommandRequest request);
        void RegisterHandler(ICommandHandler handler);
    }
}
