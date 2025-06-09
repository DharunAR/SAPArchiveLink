using Microsoft.AspNetCore.Mvc;

namespace SAPArchiveLink
{
    public class ALCommandDispatcher : ICommandDispatcherService
    {
        private readonly Dictionary<ALCommandTemplate, ICommandHandler> _handlers = new();
        ICommandResponseFactory _commandResponseFactory;

        public ALCommandDispatcher(IEnumerable<ICommandHandler> commandHandlers, ICommandResponseFactory commandResponseFactory)
        {
            foreach (var handler in commandHandlers)
            {
                RegisterHandler(handler);
            }

            _commandResponseFactory = commandResponseFactory;
        }

        public void RegisterHandler(ICommandHandler handler)
        {
            _handlers[handler.CommandTemplate] = handler;
        }

        public async Task<IActionResult> RunRequest(CommandRequest request, ContentServerRequestAuthenticator _authenticator)
        {
            var context = new CommandRequestContext(request.HttpRequest);
            var command = ALCommand.FromHttpRequest(new CommandRequest
            {
                Url = context.GetALQueryString(false),
                HttpMethod = context.GetHttpMethod(),
                Charset = request.Charset,
                HttpRequest = request.HttpRequest
            });

            // Authenticate the request
            // In a real application, certificates might be retrieved from a service or configuration
           // var certificates = new List<IArchiveCertificate>(); // Populate as needed
           //
           //var certificate = _authenticator.CheckRequest(request, command, certificates);

            var response = await ExecuteRequest(context, command);

            if (response.StatusCode == 307 && response.Headers.TryGetValue("Location", out string? locationUrl))
            {
                return new RedirectResult(locationUrl, false);
            }

            return new ArchiveLinkResult(response);
        }

        private async Task<ICommandResponse> ExecuteRequest(ICommandRequestContext context, ICommand command)
        {
            try
            {
                bool doForward = Environment.GetEnvironmentVariable("FORWARD_CONTENT_TO_KNOWNSERVER")?.Trim().ToLower() == "true";
                if (doForward && (command.IsHttpPOST() || command.IsHttpPUT()))
                {
                    string redirectUrl = $"https://{context.GetServerName()}:{context.GetPort()}/{context.GetContextPath()}?{context.GetALQueryString(false)}";

                    var redirectResponse = CommandResponse.ForProtocolText(string.Empty, 307);
                    redirectResponse.Headers["Location"] = redirectUrl;
                    return redirectResponse;
                }

                if (!_handlers.TryGetValue(command.GetTemplate(), out var handler))
                {
                    return _commandResponseFactory.CreateError($"Unsupported command: {command.GetTemplate()} for HTTP method {context.GetHttpMethod()}");
                }

                return await handler.HandleAsync(command, context);
            }
            catch (ALException ex)
            {
                return _commandResponseFactory.CreateError(ex.Message);
            }
            catch (Exception ex)
            {
                return _commandResponseFactory.CreateError($"An unexpected internal server error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
