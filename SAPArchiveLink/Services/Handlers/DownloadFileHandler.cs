using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace SAPArchiveLink
{
    public class DownloadFileHandler
    {
        private readonly string _saveDirectory;

        public DownloadFileHandler(string saveDirectory)
        {
            _saveDirectory = saveDirectory;
        }

        public async Task<List<SapDocumentComponent>> HandleRequestAsync(HttpRequest request)
        {
            var uploadedFiles = new List<SapDocumentComponent>();

            try
            {
                // ✅ Try ReadFormAsync (standard)
                var form = await request.ReadFormAsync();

                foreach (var file in form.Files)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(_saveDirectory, fileName);

                    Directory.CreateDirectory(_saveDirectory);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                  
                    var compId = form.TryGetValue("X-compId", out var compIdValue) ? compIdValue.ToString() : string.Empty;

                    uploadedFiles.Add(new SapDocumentComponent
                    {
                        FileName = filePath,
                        CompId = compId,
                        ContentType = file.ContentType
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadFormAsync failed: " + ex.Message);
                uploadedFiles = await ParseMultipartManuallyAsync(request);
            }

            return uploadedFiles;
        }

        private async Task<List<SapDocumentComponent>> ParseMultipartManuallyAsync(HttpRequest request)
        {
            var uploadedFiles = new List<SapDocumentComponent>();
            var boundary = GetBoundaryFromContentType(request.ContentType);
            if (string.IsNullOrEmpty(boundary))
                throw new InvalidOperationException("Boundary not found in Content-Type.");

            var reader = new MultipartReader(boundary, request.Body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) &&
                    contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    var fileName = Path.GetFileName(contentDisposition.FileName.Value.Trim('"'));
                    var filePath = Path.Combine(_saveDirectory, fileName);

                    Directory.CreateDirectory(_saveDirectory);
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await section.Body.CopyToAsync(fileStream);

                    // Fix for CS8602: Ensure section.Headers is not null before accessing it
                    if (section.Headers != null)
                    {
                        section.Headers.TryGetValue("X-compId", out var compId);
                        section.Headers.TryGetValue("X-Content-Type", out var xContentType);

                        uploadedFiles.Add(new SapDocumentComponent
                        {
                            FileName = fileName,
                            ContentType = xContentType.ToString()
                        });
                    }
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
    }
}
