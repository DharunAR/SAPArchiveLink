using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text;

namespace SAPArchiveLink
{
    public class ArchiveLinkResult : IActionResult
    {
        private readonly CommandResponse _response;

        public ArchiveLinkResult(CommandResponse response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var httpResponse = context.HttpContext.Response;
            httpResponse.StatusCode = _response.StatusCode;
            httpResponse.ContentType = _response.ContentType;

            foreach (var header in _response.Headers)
            {
                httpResponse.Headers[header.Key] = header.Value;
            }
            if (!_response.IsStream && _response.TextContent != null)
            {
                httpResponse.Headers.ContentLength = System.Text.Encoding.UTF8.GetByteCount(_response.TextContent);
            }
            if (_response.IsStream && _response.StreamContent != null)
            {
                if (_response.StreamContent.CanSeek)
                {
                    httpResponse.ContentLength = _response.StreamContent.Length;
                    _response.StreamContent.Position = 0;
                }

                await _response.StreamContent.CopyToAsync(httpResponse.Body);
            }
            else if (!_response.IsStream && _response.TextContent != null)
            {
                httpResponse.ContentLength = Encoding.UTF8.GetByteCount(_response.TextContent);
                await httpResponse.WriteAsync(_response.TextContent);
            }
        }
    }
}
