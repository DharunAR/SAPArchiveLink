using Microsoft.AspNetCore.Mvc;
using SAPArchiveLink.Resources;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SAPArchiveLink.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContentServerController : ControllerBase
    {
        private readonly ICommandDispatcherService _dispatcher;
        private readonly ContentServerRequestAuthenticator _authenticator;

        public ContentServerController(ICommandDispatcherService dispatcher,ContentServerRequestAuthenticator authenticator)
        {
            _dispatcher = dispatcher;
            _authenticator = authenticator;
        }

        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public async Task<IActionResult> Handle()
        {
            try
            {
                string queryString = Request.QueryString.Value?.TrimStart('?') ?? "";

                if (string.IsNullOrWhiteSpace(queryString))
                {
                    return Ok(new ArchiveLinkStatusResponse()
                    {
                        Message = Resource.ArchiveLinkRunning,
                        Status = Resource.Ok,
                        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                    });
                }

                string contentType = Request.ContentType ?? "";
                string charset = "UTF-8";

                if (contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(contentType, @"charset=([^\s;]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        charset = match.Groups[1].Value.Trim();
                    }
                }

                var commandRequest = new CommandRequest
                {
                    Url = queryString,
                    HttpMethod = Request.Method,
                    Charset = charset,
                    HttpRequest = Request
                };

                return await _dispatcher.RunRequest(commandRequest, _authenticator);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ArchiveLinkStatusResponse(){ Message = string.Format(Resource.UnExpectedError, ex.Message), Status = Resource.ServerError });
            }
        }
    }
}
