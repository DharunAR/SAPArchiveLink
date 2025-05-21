using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace SAPArchiveLink.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContentServerController : ControllerBase
    {
        private readonly ICommandDispatcherService _dispatcher;
        private readonly ContentServerRequestAuthenticator _authenticator;

        public ContentServerController(ICommandDispatcherService dispatcher, 
                ContentServerRequestAuthenticator authenticator)
        {
            _dispatcher = dispatcher;
            _authenticator = authenticator;
        }        

        [HttpGet("/ContentServer")]
        [HttpPost("/ContentServer")]
        [HttpPut("/ContentServer")]       
        [HttpDelete("/ContentServer")]
        public async Task<IActionResult> Handle()
        {
            try
            {
                string queryString = Request.QueryString.Value??"";
                if (string.IsNullOrEmpty(queryString))
                {
                    return BadRequest("Query string is required.");
                }
                queryString = queryString.StartsWith("?") ? queryString.Substring(1) : queryString;

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
            catch (ALException ex)
            {
                return StatusCode(400, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
