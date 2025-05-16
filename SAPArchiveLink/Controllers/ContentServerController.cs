using Microsoft.AspNetCore.Mvc;
using SAPArchiveLink.Helpers;
using SAPArchiveLink.Models;
using SAPArchiveLink.Services;
using System.Text.RegularExpressions;

namespace SAPArchiveLink.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContentServerController : ControllerBase
    {
        private readonly ICommandDispatcherService _dispatcher;

        public ContentServerController(ICommandDispatcherService dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet, HttpPost, HttpPut, HttpDelete]
        public async Task<IActionResult> Handle()
        {
            try
            {
                string queryString = Request.QueryString.Value;
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

                return await _dispatcher.RunRequest(commandRequest);
            }
            catch (ALException ex)
            {
                return StatusCode(ex.StatusCode ?? 400, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
