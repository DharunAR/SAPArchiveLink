using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace SAPArchiveLink
{
    public class DownloadFileHandler: IDownloadFileHandler
    {
        private readonly string _saveDirectory;
        private readonly IOptionsMonitor<TrimConfigSettings> _config;

        public DownloadFileHandler(IOptionsMonitor<TrimConfigSettings> config)
        {
            _config = config;
            _saveDirectory = config.CurrentValue.WorkPath ?? throw new InvalidOperationException("WorkPath is not set in TRIMConfig.");
        }

        private string NormalizeContentType(string contentType)
        {
            if (contentType.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                return contentType.Substring("Content-Type:".Length).Trim();
            return contentType;
        }

        public async Task<List<SapDocumentComponentModel>> HandleRequestAsync(string contentType, Stream body, string docId)
        {
            var uploadedFiles = new List<SapDocumentComponentModel>();
            contentType = NormalizeContentType(contentType);

            try
            {
                if (!body.CanSeek)
                {
                    var buffered = new MemoryStream();
                    await body.CopyToAsync(buffered);
                    buffered.Position = 0;
                    body = buffered;
                }
                else
                {
                    body.Position = 0;
                    body.Seek(0, SeekOrigin.Begin);
                }

                if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
                {
                    uploadedFiles = await ParseSinglepartManuallyAsync(contentType, body, docId);
                }
                else
                {
                    uploadedFiles = await ParseMultipartManuallyAsync(contentType, body);
                }

            }
            catch(Exception ex)
            {
                throw;
            }

            return uploadedFiles;
        }

        /// <summary>
        /// Downloads a document from the provided stream and saves it to the specified file path.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<string> DownloadDocument(Stream stream, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
            return filePath;
        }

        /// <summary>
        /// Deletes a file at the specified file path if it exists.
        /// </summary>
        /// <param name="filePath"></param>
        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                }
            }

        }

        private string? GetExtensionFromContentType(string contentType)
        {
            var provider = new FileExtensionContentTypeProvider();

            var customMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "application/x-note", ".note" },
                        { "text/plain", ".txt" },
                        { "application/json", ".json" },
                        { "application/pdf", ".pdf" }
                        // Add more as needed
                    };
            if (customMappings.TryGetValue(contentType, out var ext))
            {
                return ext;
            }

            var mapping = provider.Mappings
                .FirstOrDefault(kvp => kvp.Value.Equals(contentType, StringComparison.OrdinalIgnoreCase));

            return !string.IsNullOrEmpty(mapping.Key) ? mapping.Key : ".bin";
        }

        private Task<List<SapDocumentComponentModel>> ParseSinglepartManuallyAsync(string contentType, Stream body, string docId)
        {
            var uploadedFiles = new List<SapDocumentComponentModel>();
            try
            {
                var filePath = getFilePath(docId, contentType);
                uploadedFiles.Add(new SapDocumentComponentModel
                {
                    FileName = filePath,
                    Data = body,
                    ContentType = contentType,
                });
            }
            catch
            {
            }

            return Task.FromResult(uploadedFiles);
        }

        private string getFilePath(string compId, string contentType)
        {
            var fileName = $"{compId}{GetExtensionFromContentType(contentType)}";
            return Path.Combine(_saveDirectory, fileName);
        }

        private string ExtractCharset(string xContentType)
        {
            if (string.IsNullOrEmpty(xContentType))
                return null;

            var parts = xContentType.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("charset=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring("charset=".Length).Trim();
                }
            }

            return null; // no charset found
        }

        private async Task<List<SapDocumentComponentModel>> ParseMultipartManuallyAsync(string contentType, Stream body)
        {
            var uploadedFiles = new List<SapDocumentComponentModel>();
            var boundary = GetBoundaryFromContentType(contentType);
            if (string.IsNullOrEmpty(boundary))
                throw new InvalidOperationException("Boundary not found in Content-Type.");

            var reader = new MultipartReader(boundary, body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                string? filePath = null;
                string? fileName = null;

                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) &&
                    contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    fileName = Path.GetFileName(contentDisposition.FileName.Value.Trim('"'));
                    if (Path.HasExtension(fileName))
                    {
                        filePath = Path.Combine(_saveDirectory, fileName);
                    }
                }

                if (section.Headers != null)
                {
                    section.Headers.TryGetValue("X-compId", out var compId);
                    section.Headers.TryGetValue("X-docId", out var docId);
                    section.Headers.TryGetValue("Content-Type", out var contentTypeHeader);
                    section.Headers.TryGetValue("X-pVersion", out var pVersion);
                    section.Headers.TryGetValue("X-Content-Length", out var contentLength);

                    if (filePath == null)
                    {
                        filePath = getFilePath(fileName == null ? compId.FirstOrDefault() ?? string.Empty : fileName, contentTypeHeader.ToString());
                    }

                    var buffered = new MemoryStream();
                    await section.Body.CopyToAsync(buffered);
                    buffered.Position = 0;

                    uploadedFiles.Add(new SapDocumentComponentModel
                    {
                        DocId = docId.FirstOrDefault() ?? string.Empty,
                        CompId = compId.FirstOrDefault() ?? string.Empty,
                        Data = buffered,
                        FileName = filePath,
                        ContentType = contentTypeHeader.ToString(),
                        Charset = ExtractCharset(contentTypeHeader.ToString()),
                        PVersion = pVersion.FirstOrDefault() ?? string.Empty,
                        ContentLength = long.TryParse(contentLength.FirstOrDefault(), out var parsedLength) ? parsedLength : 0
                    });
                }
            }

            return uploadedFiles;
        }

        private string GetBoundaryFromContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType), "Content-Type cannot be null or empty.");
            }

            var elements = contentType.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var boundaryElement = Array.Find(elements, e => e.Trim().StartsWith("boundary=", StringComparison.OrdinalIgnoreCase));
            return boundaryElement?.Split('=')[1].Trim('"') ?? throw new InvalidOperationException("Boundary not found in Content-Type.");
        }

        public void ClearAllFiles(string? filePath = null)
        {
            string filesToClear = filePath ?? $"{_saveDirectory}\\Uploads";
            if (Directory.Exists(filesToClear))
            {
                var files = Directory.GetFiles(filesToClear);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                    }
                }
            }
        }

       
    }
}
