using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text;

namespace SAPArchiveLink
{
    public class ArchiveLinkResult : IActionResult
    {
        private readonly ICommandResponse _response;

        public ArchiveLinkResult(ICommandResponse response)
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

            if (_response.IsStream && _response.Components != null)
            {
                // Handle multipart/form-data response
                string boundary = _response.Boundary;

                var encoding = Encoding.UTF8;
                var writer = new StreamWriter(httpResponse.Body, encoding, leaveOpen: true);

                if (_response.Components.Count == 0)
                {
                    // Empty document response
                    await writer.WriteAsync($"--{boundary}--\r\n");
                    await writer.FlushAsync();
                    return;
                }

                foreach (var component in _response.Components)
                {
                    await writer.WriteAsync($"--{boundary}\r\n");

                    string contentType = component.ContentType;
                    if (!string.IsNullOrEmpty(component.Charset))
                        contentType += $"; charset={component.Charset}";
                    if (!string.IsNullOrEmpty(component.Version))
                        contentType += $"; version={component.Version}";

                    await writer.WriteAsync($"Content-Type: {contentType}\r\n");
                    await writer.WriteAsync($"Content-Length: {component.ContentLength}\r\n");
                    await writer.WriteAsync($"X-Content-Length: {component.ContentLength}\r\n");
                    await writer.WriteAsync($"X-compId: {component.CompId}\r\n");
                    await writer.WriteAsync($"X-compDateC: {component.CreationDate:yyyy-MM-dd}\r\n");
                    await writer.WriteAsync($"X-compTimeC: {component.CreationDate:HH:mm:ss}\r\n");
                    await writer.WriteAsync($"X-compDateM: {component.ModifiedDate:yyyy-MM-dd}\r\n");
                    await writer.WriteAsync($"X-compTimeM: {component.ModifiedDate:HH:mm:ss}\r\n");
                    await writer.WriteAsync($"X-compStatus: {component.Status}\r\n");
                    await writer.WriteAsync($"X-pVersion: {component.PVersion}\r\n");
                    await writer.WriteAsync("\r\n"); // end of headers
                    await writer.FlushAsync();

                    // Write binary content
                    if (component.Data.CanSeek)
                        component.Data.Position = 0;

                    await component.Data.CopyToAsync(httpResponse.Body);
                    await writer.WriteAsync("\r\n");
                    await writer.FlushAsync();
                }

                await writer.WriteAsync($"--{boundary}--\r\n");
                await writer.FlushAsync();
            }
            else if (_response.IsStream && _response.StreamContent != null)
            {
                if (_response.StreamContent.CanSeek)
                {
                    _response.StreamContent.Position = 0;
                    httpResponse.ContentLength = _response.StreamContent.Length;
                }

                await _response.StreamContent.CopyToAsync(httpResponse.Body);
            }
            else if (!_response.IsStream && _response.TextContent != null)
            {
                var encoding = Encoding.UTF8;
                httpResponse.ContentLength = encoding.GetByteCount(_response.TextContent);
                await httpResponse.WriteAsync(_response.TextContent);
            }
        }
    }
}
