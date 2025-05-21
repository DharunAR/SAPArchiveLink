using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

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

            // Add custom headers from CommandResponse
            foreach (var header in _response.Headers)
            {
                httpResponse.Headers[header.Key] = header.Value;
            }
            if (_response.IsBinary && _response.BinaryContent != null)
            {
                httpResponse.Headers.ContentLength = _response.BinaryContent.Length;
            }
            else if (!_response.IsBinary && _response.TextContent != null)
            {
                httpResponse.Headers.ContentLength = System.Text.Encoding.UTF8.GetByteCount(_response.TextContent);
            }
            // Write the content to the response body
            if (_response.IsBinary)
            {
                if (_response.BinaryContent != null)
                {
                    await httpResponse.Body.WriteAsync(_response.BinaryContent, 0, _response.BinaryContent.Length);
                }
            }
            else // Text content
            {
                if (_response.TextContent != null)
                {
                    await httpResponse.WriteAsync(_response.TextContent);
                }
            }
        }
    }
}
