using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace SAPArchiveLink
{
    public class ArchiveLinkResult : IActionResult
    {
        private readonly ICommandResponse _response;
        private readonly IDownloadFileHandler? _fileHandler;

        public ArchiveLinkResult(ICommandResponse response, IDownloadFileHandler? downloadFileHandler = null)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _fileHandler = downloadFileHandler;
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
            // Multipart document (docGet, get)
            if (_response.IsStream && _response.Components != null && _response.StreamContent is null)
            {
                string boundary = _response.Boundary;

                foreach (var component in _response.Components)
                {
                    await WriteComponentPart(httpResponse, component, boundary, includeContent: true);
                }

                await httpResponse.Body.WriteAsync(Encoding.ASCII.GetBytes($"--{boundary}--\r\n"));
            }
            // Single stream (get binary)
            else if (_response.IsStream && _response.StreamContent != null)
            {
                if (_response.StreamContent.CanSeek)
                {
                    _response.StreamContent.Position = 0;
                    httpResponse.ContentLength = _response.StreamContent.Length;
                }

                await _response.StreamContent.CopyToAsync(httpResponse.Body);
            }
            // Metadata-only multipart (info command)
            else if (!_response.IsStream && _response.Components?.Count > 0)
            {
                string boundary = _response.Boundary;

                foreach (var component in _response.Components)
                {
                    await WriteComponentPart(httpResponse, component, boundary, includeContent: false);
                }

                await httpResponse.Body.WriteAsync(Encoding.ASCII.GetBytes($"--{boundary}--\r\n"));
            }
            // Plain text or HTML
            else if (!_response.IsStream && _response.TextContent != null)
            {
                var encoding = Encoding.UTF8;
                httpResponse.ContentLength = encoding.GetByteCount(_response.TextContent);
                await httpResponse.WriteAsync(_response.TextContent);
            }
        }

        private async Task WriteComponentPart(HttpResponse response, SapDocumentComponentModel component, string boundary, bool includeContent)
        {
            try
            {
                var contentLength = !includeContent ? 0 : component.ContentLength;
                var headers = new StringBuilder();
                headers.AppendLine($"--{boundary}");

                string contentType = component.ContentType ?? "application/octet-stream";
                if (!string.IsNullOrEmpty(component.Charset))
                    contentType += $"; charset={component.Charset}";
                if (!string.IsNullOrEmpty(component.Version))
                    contentType += $"; version={component.Version}";

                headers.AppendLine($"Content-Type: {contentType}");
                headers.AppendLine($"Content-Length: {contentLength}");
                headers.AppendLine($"X-Content-Length: {component.ContentLength}");
                headers.AppendLine($"X-compId: {component.CompId}");
                headers.AppendLine($"X-compDateC: {component.CreationDate:yyyy-MM-dd}");
                headers.AppendLine($"X-compTimeC: {component.CreationDate:HH:mm:ss}");
                headers.AppendLine($"X-compDateM: {component.ModifiedDate:yyyy-MM-dd}");
                headers.AppendLine($"X-compTimeM: {component.ModifiedDate:HH:mm:ss}");
                headers.AppendLine($"X-compStatus: {component.Status}");
                headers.AppendLine($"X-pVersion: {component.PVersion}");
                headers.AppendLine();

                byte[] headerBytes = Encoding.ASCII.GetBytes(headers.ToString());
                await response.Body.WriteAsync(headerBytes);

                if (includeContent && component.Data != null)
                {
                    if (component.Data.CanSeek)
                    {
                        component.Data.Seek(0, SeekOrigin.Begin);
                    }

                    byte[] buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await component.Data.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await response.Body.WriteAsync(buffer, 0, bytesRead);
                    }

                    await response.Body.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));
                }
            }
            finally
            {
                ClearExtractedFiles(component.FileName);
            }
        }

        private void ClearExtractedFiles(string fileName)
        {
            try
            {
                if (_fileHandler != null)
                {
                    // If a file handler is provided, delete the file after writing to the response
                    if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
                    {
                        _fileHandler.DeleteFile(fileName);
                    }
                }
            }
            catch (Exception)
            {
               //Do nothing
            }

        }
    }
}
