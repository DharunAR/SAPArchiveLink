using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.Mime;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SAPArchiveLink
{
    public class DownloadFileHandler
    {
        private readonly string _saveDirectory;

        public DownloadFileHandler(string saveDirectory)
        {
            _saveDirectory = saveDirectory;
        }

        public async Task<List<SapDocumentComponent>> HandleRequestAsync(string contentType, Stream body, string docId) 
        {
            var uploadedFiles = new List<SapDocumentComponent>();           

            try
            {
                if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
                {
                    uploadedFiles = await ParseSinglepartManuallyAsync(contentType, body, docId);
                }
                else
                {
                    uploadedFiles = await ParseMultipartManuallyAsync(contentType, body);
                }               
              
            }
            catch
            {            
               
            }

            return uploadedFiles;
        }

        public string? GetExtensionFromContentType(string contentType)
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

        private Task<List<SapDocumentComponent>> ParseSinglepartManuallyAsync(string contentType, Stream body, string docId)
        {
            var uploadedFiles = new List<SapDocumentComponent>();
            try
            {               
                var filePath = getFilePath(docId, contentType);
                uploadedFiles.Add(new SapDocumentComponent
                {
                    FileName = filePath,
                    Data = body,
                    ContentType= contentType,
                });
            }
            catch
            {
            }

            return Task.FromResult(uploadedFiles);
        }

        private string getFilePath(string compId,string contentType)
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
        private async Task<List<SapDocumentComponent>> ParseMultipartManuallyAsync(string contentType, Stream body)
        {
            var uploadedFiles = new List<SapDocumentComponent>();
           
            var boundary = GetBoundaryFromContentType(contentType);
            if (string.IsNullOrEmpty(boundary))
                throw new InvalidOperationException("Boundary not found in Content-Type.");

            var reader = new MultipartReader(boundary, body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                string filePath = null;
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) &&
                    contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    var fileName = Path.GetFileName(contentDisposition.FileName.Value.Trim('"'));
                    filePath = Path.Combine(_saveDirectory, fileName);
                }                                          

                if (section.Headers != null)
                {
                    section.Headers.TryGetValue("X-compId", out var compId);
                    section.Headers.TryGetValue("Content-Type", out var contentTypeHeader);
                    section.Headers.TryGetValue("X-pVersion", out var pVersion);
                    section.Headers.TryGetValue("X-Content-Length", out var contentLength);
                    if(filePath==null)
                    {
                        filePath = getFilePath(compId, contentTypeHeader.ToString());
                    }

                    uploadedFiles.Add(new SapDocumentComponent
                    {
                        CompId = compId.FirstOrDefault() ?? string.Empty,
                        Data = section.Body,
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
    }
}
