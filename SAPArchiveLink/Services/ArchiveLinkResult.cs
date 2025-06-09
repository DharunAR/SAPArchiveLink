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

            if (_response.IsStream && _response.Components != null && _response.StreamContent is null)
            {
                string boundary = _response.Boundary;

                if (_response.Components.Count == 0)
                {
                    var footer = Encoding.ASCII.GetBytes($"--{boundary}--\r\n");
                    await httpResponse.Body.WriteAsync(footer);
                    return;
                }

                foreach (var component in _response.Components)
                {
                    // Ensure stream is ready to read from the beginning
                    if (component.Data.CanSeek)
                    {
                        component.Data.Seek(0, SeekOrigin.Begin);
                    }
                    else
                    {
                        // Buffer non-seekable stream
                        var buffered = new MemoryStream();
                        await component.Data.CopyToAsync(buffered);
                        buffered.Position = 0;
                        component.Data = buffered;
                    }

                    // Build and write headers
                    var headers = new StringBuilder();
                    headers.AppendLine($"--{boundary}");

                    string contentType = component.ContentType;
                    if (!string.IsNullOrEmpty(component.Charset))
                        contentType += $"; charset={component.Charset}";
                    if (!string.IsNullOrEmpty(component.Version))
                        contentType += $"; version={component.Version}";

                    headers.AppendLine($"Content-Type: {contentType}");
                    headers.AppendLine($"Content-Length: {component.ContentLength}");
                    headers.AppendLine($"X-Content-Length: {component.ContentLength}");
                    headers.AppendLine($"X-compId: {component.CompId}");
                    headers.AppendLine($"X-compDateC: {component.CreationDate:yyyy-MM-dd}");
                    headers.AppendLine($"X-compTimeC: {component.CreationDate:HH:mm:ss}");
                    headers.AppendLine($"X-compDateM: {component.ModifiedDate:yyyy-MM-dd}");
                    headers.AppendLine($"X-compTimeM: {component.ModifiedDate:HH:mm:ss}");
                    headers.AppendLine($"X-compStatus: {component.Status}");
                    headers.AppendLine($"X-pVersion: {component.PVersion}");
                    headers.AppendLine(); // End of headers

                    byte[] headerBytes = Encoding.ASCII.GetBytes(headers.ToString());
                    await httpResponse.Body.WriteAsync(headerBytes, 0, headerBytes.Length);

                    // Write binary content
                    byte[] buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await component.Data.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await httpResponse.Body.WriteAsync(buffer, 0, bytesRead);
                    }

                    await httpResponse.Body.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));
                    await httpResponse.Body.FlushAsync();
                }

                // Final boundary
                var finalBoundary = Encoding.ASCII.GetBytes($"--{boundary}--\r\n");
                await httpResponse.Body.WriteAsync(finalBoundary);
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
