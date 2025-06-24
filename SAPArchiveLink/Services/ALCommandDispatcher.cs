using Microsoft.AspNetCore.Mvc;

namespace SAPArchiveLink
{
    public class ALCommandDispatcher : ICommandDispatcherService
    {
        private readonly ICommandHandlerRegistry _handlerRegistry;
        private readonly ICommandResponseFactory _commandResponseFactory;

        public ALCommandDispatcher(ICommandHandlerRegistry handlerRegistry, ICommandResponseFactory commandResponseFactory)
        {
            _handlerRegistry = handlerRegistry;
            _commandResponseFactory = commandResponseFactory;
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

            if (!command.IsValid)
            {
                var errorResponse = _commandResponseFactory.CreateError($"Bad request: {command.ValidationError}", StatusCodes.Status400BadRequest);
                return new ArchiveLinkResult(errorResponse);
            }

            // TODO: Uncomment when you implement auth
            // var certificates = new List<IArchiveCertificate>();
            // var certificate = _authenticator.CheckRequest(request, command, certificates);

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

                var handler = _handlerRegistry.GetHandler(command.GetTemplate());

                if (handler == null)
                {
                    return _commandResponseFactory.CreateError($"Unsupported command: {command.GetTemplate()} for HTTP method {context.GetHttpMethod()}");
                }

                return await handler.HandleAsync(command, context);
            }
            catch (Exception ex)
            {
                return _commandResponseFactory.CreateError($"Internal server error: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
