using Microsoft.AspNetCore.Mvc;
using SAPArchiveLink.Resources;

namespace SAPArchiveLink
{
    public class ALCommandDispatcher : ICommandDispatcherService
    {
        private readonly ICommandHandlerRegistry _handlerRegistry;
        private readonly ICommandResponseFactory _commandResponseFactory;
        private readonly IDownloadFileHandler _downloadFileHandler;
        private readonly IDatabaseConnection _databaseConnection;
        private readonly ILogHelper<ALCommandDispatcher> _logger;

        public ALCommandDispatcher(ICommandHandlerRegistry handlerRegistry, ICommandResponseFactory commandResponseFactory, 
            IDownloadFileHandler downloadFileHandler, IDatabaseConnection databaseConnection, ILogHelper<ALCommandDispatcher> logger)
        {
            _handlerRegistry = handlerRegistry;
            _commandResponseFactory = commandResponseFactory;
            _downloadFileHandler = downloadFileHandler;
            _databaseConnection = databaseConnection;
            _logger = logger;
        }

        public async Task<IActionResult> RunRequest(CommandRequest request, ContentServerRequestAuthenticator _authenticator)
        {
            var skipAuthTemplates = new[]
            {
                ALCommandTemplate.PUTCERT,
                ALCommandTemplate.SERVERINFO
            };
            var context = new CommandRequestContext(request.HttpRequest);

            var command = ALCommand.FromHttpRequest(new CommandRequest
            {
                Url = context.GetALQueryString(false),
                HttpMethod = context.GetHttpMethod(),
                Charset = request.Charset,
                HttpRequest = request.HttpRequest
            });
            var repository = command.GetValue(ALParameter.VarContRep);
            if (!command.IsValid)
            {
                var errorResponse = _commandResponseFactory.CreateError(string.Format(Resource.Badrequest, command.ValidationError), StatusCodes.Status400BadRequest);
                _logger.LogError($"Invalid command: {command.ValidationError}");
                return new ArchiveLinkResult(errorResponse);
            }

            using var trimRepo = _databaseConnection.GetDatabase();
            if (!trimRepo.IsProductFeatureActivated())
            {
                var errorRes = _commandResponseFactory.CreateError(Resource.LicenseNotEnabled, StatusCodes.Status403Forbidden);
                _logger.LogError("Product feature is not activated.");
                return new ArchiveLinkResult(errorRes);
            }

            if (!skipAuthTemplates.Contains(command.GetTemplate()) && !string.IsNullOrEmpty(repository))
            {
                var archiveCertificate = trimRepo.GetArchiveCertificate(repository);
                if (archiveCertificate == null)
                {
                    var errorResponse = _commandResponseFactory.CreateError(string.Format(Resource.CertificateNotFound, repository), StatusCodes.Status404NotFound);
                   _logger.LogError($"Archive certificate not found for repository: {repository}");
                    return new ArchiveLinkResult(errorResponse);
                }
                if (!archiveCertificate.IsEnabled())
                {
                    var errorResponse = _commandResponseFactory.CreateError(string.Format(Resource.CertificateNotEnabled, repository), StatusCodes.Status403Forbidden);
                    _logger.LogError($"Archive certificate is not enabled for repository: {repository}");
                    return new ArchiveLinkResult(errorResponse);
                }
                var requestAuthResult = _authenticator.CheckRequest(request, command, archiveCertificate);
                if (requestAuthResult != null && !requestAuthResult.IsAuthenticated)
                {
                    _logger.LogError($"Authentication failed for command {command.GetTemplate()}: {requestAuthResult.ErrorResponse}");
                    return new ArchiveLinkResult(requestAuthResult.ErrorResponse);
                }
            }
            else if (command.GetTemplate() == ALCommandTemplate.SERVERINFO)
            {
                var pVersion = command.GetValue(ALParameter.VarPVersion);
                if (string.IsNullOrEmpty(pVersion) || !_authenticator.IsSupportedVersion(pVersion))
                {
                    string version = pVersion ?? "none";
                    string errorMessage = string.Format(Resource.UnsupportedVersion, version);

                    var errorResponse = _commandResponseFactory.CreateError(errorMessage, StatusCodes.Status400BadRequest);
                    _logger.LogError(errorMessage);
                    return new ArchiveLinkResult(errorResponse);
                }
            }

            var response = await ExecuteRequest(context, command);

            _logger.LogInformation($"Command {command.GetTemplate()} executed successfully with status code {response.StatusCode}");
            return new ArchiveLinkResult(response, _downloadFileHandler);
        }

        private async Task<ICommandResponse> ExecuteRequest(ICommandRequestContext context, ICommand command)
        {
            try
            {
                var handler = _handlerRegistry.GetHandler(command.GetTemplate());

                if (handler == null)
                {
                    _logger.LogError($"No handler found for command template: {command.GetTemplate()}");
                    return _commandResponseFactory.CreateError(string.Format(Resource.UnsupportedCommand, command.GetTemplate(), context.GetHttpMethod()));
                }

                return await handler.HandleAsync(command, context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing command {command.GetTemplate()}: {ex.Message}",ex);
                return _commandResponseFactory.CreateError(string.Format(Resource.Error_InternalServer, ex.Message), StatusCodes.Status500InternalServerError);
            }
        }
    }
}
