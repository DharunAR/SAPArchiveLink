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
                                      ICommandResponseFactory responseFactory)
        {
            if (!initState.IsInitialized)
            {
                var response = responseFactory.CreateError(initState?.ErrorMessage,StatusCodes.Status500InternalServerError);
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
