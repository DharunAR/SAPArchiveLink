using Microsoft.AspNetCore.Mvc;

namespace SAPArchiveLink
{
    public class ALCommandDispatcher : ICommandDispatcherService
    {
        private readonly Dictionary<ALCommandTemplate, ICommandHandler> _handlers = new();

        public ALCommandDispatcher(IEnumerable<ICommandHandler> commandHandlers)
        {
            foreach (var handler in commandHandlers)
            {
                RegisterHandler(handler);
            }
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
            var certificates = new List<IArchiveCertificate>(); // Populate as needed
            var certificate = _authenticator.CheckRequest(request, command, certificates);

            var response = await ExecuteRequest(context, command);

            if (response.StatusCode == 307 && response.Headers.ContainsKey("Location"))
            {
                return new RedirectResult(response.Headers["Location"], false);
            }

            foreach (var header in response.Headers)
            {
                if (header.Key != "Location")
                {
                    request.HttpRequest.HttpContext.Response.Headers[header.Key] = header.Value;
                }
            }

            return new ObjectResult(response.Content)
            {
                StatusCode = response.StatusCode
            };
        }

        private async Task<CommandResponse> ExecuteRequest(ICommandRequestContext context, ICommand command)
        {
            try
            {
                bool doForward = Environment.GetEnvironmentVariable("FORWARD_CONTENT_TO_KNOWNSERVER")?.Trim().ToLower() == "true";
                if (doForward && (command.IsHttpPOST() || command.IsHttpPUT()))
                {
                    string redirectUrl = $"https://{context.GetServerName()}:{context.GetPort()}/{context.GetContextPath()}?{context.GetALQueryString(false)}";
                    return new CommandResponse($"Redirect to {redirectUrl}")
                    {
                        StatusCode = 307,
                        Headers = { { "Location", redirectUrl } }
                    };
                }

                if (!_handlers.TryGetValue(command.GetTemplate(), out var handler))
                {
                    return new CommandResponse($"Unsupported command: {command.GetTemplate()} for HTTP method {context.GetHttpMethod()}")
                    {
                        StatusCode = 400
                    };
                }

                return await handler.HandleAsync(command, context);
            }
            catch (ALException ex)
            {
                return new CommandResponse(ex.Message) { StatusCode = 400 };
            }
            catch (Exception ex)
            {
                return new CommandResponse($"Unexpected error: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
