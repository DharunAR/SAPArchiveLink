using Microsoft.Extensions.Options;

namespace SAPArchiveLink
{
    /// <summary>
    /// Middleware class to handle Trim initialization exceptions
    /// Returns server error with Trim exception messages.
    /// </summary>
    public class TrimApplicationMiddleware
    {
        private readonly RequestDelegate _next;

        public TrimApplicationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context,
                                      TrimInitialization initState,
                                      IOptionsMonitor<TrimConfigSettings> configMonitor,
                                      ICommandResponseFactory responseFactory, ILogHelper<TrimApplicationMiddleware> _logger)
        {
            if (!initState.IsInitialized)
            {
                try
                {
                    TrimServiceInitializer.InitializeTrimService(configMonitor, initState);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"TrimServiceInitializer Failed: {ex.Message}");
                }
            }

            if (!initState.IsInitialized)
            {
                var errorMessage = initState?.ErrorMessage ?? "TRIM initialization failed.";
                _logger.LogError(errorMessage);
                var response = responseFactory.CreateError(errorMessage, StatusCodes.Status500InternalServerError);
                context.Response.StatusCode = response.StatusCode;
                context.Response.ContentType = response.ContentType;

                if (response.TextContent is string text)
                {
                    await context.Response.WriteAsync(text);
                }

                return;
            }

            await _next(context);
        }
    }

}
