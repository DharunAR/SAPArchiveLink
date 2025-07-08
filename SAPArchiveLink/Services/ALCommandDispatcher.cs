using Microsoft.AspNetCore.Mvc;

namespace SAPArchiveLink
{
    public class ALCommandDispatcher : ICommandDispatcherService
    {
        private readonly ICommandHandlerRegistry _handlerRegistry;
        private readonly ICommandResponseFactory _commandResponseFactory;
        private readonly IDownloadFileHandler _downloadFileHandler;
        private readonly IDatabaseConnection _databaseConnection;

        public ALCommandDispatcher(ICommandHandlerRegistry handlerRegistry, ICommandResponseFactory commandResponseFactory, IDownloadFileHandler downloadFileHandler, IDatabaseConnection databaseConnection)
        {
            _handlerRegistry = handlerRegistry;
            _commandResponseFactory = commandResponseFactory;
            _downloadFileHandler = downloadFileHandler;
            _databaseConnection = databaseConnection;
        }

        public async Task<IActionResult> RunRequest(CommandRequest request, ContentServerRequestAuthenticator _authenticator)
        {
            var skipAuthTemplates = new[]
            {
                ALCommandTemplate.PUTCERT
            };
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

            if (!skipAuthTemplates.Contains(command.GetTemplate()))
            {
                if (!string.IsNullOrEmpty(command.GetValue(ALParameter.VarContRep)))
                {
                    using var trimRepo = _databaseConnection.GetDatabase();
                    var archieveCertificate = trimRepo.GetArchiveCertificate(command.GetValue(ALParameter.VarContRep));
                    if(archieveCertificate==null)
                    {
                        var errorResponse = _commandResponseFactory.CreateError($"Archive certificate not found for repository: {command.GetValue(ALParameter.VarContRep)}", StatusCodes.Status404NotFound);
                        return new ArchiveLinkResult(errorResponse);
                    }
                    var requestAuthResult = _authenticator.CheckRequest(request, command, archieveCertificate);
                    if (requestAuthResult != null && !requestAuthResult.IsAuthenticated)
                    {
                        return new ArchiveLinkResult(requestAuthResult.ErrorResponse);
                    }
                }
            }       
          
            var response = await ExecuteRequest(context, command);

            if (response.StatusCode == 307 && response.Headers.TryGetValue("Location", out string? locationUrl))
            {
                return new RedirectResult(locationUrl, false);
            }

            return new ArchiveLinkResult(response, _downloadFileHandler);
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
