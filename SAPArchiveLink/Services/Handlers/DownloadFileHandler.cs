using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.Mime;
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
                
                //else
                //{
                //    foreach (var file in form.Files)
                //    {
                //        var fileName = Path.GetFileName(file.FileName);
                //        var filePath = Path.Combine(_saveDirectory, fileName);

                //        Directory.CreateDirectory(_saveDirectory);
                //        using var stream = new FileStream(filePath, FileMode.Create);
                //        await file.CopyToAsync(stream);

                //        var compId = form.TryGetValue("X-compId", out var compIdValue) ? compIdValue.ToString() : string.Empty;

                //        uploadedFiles.Add(new SapDocumentComponent
                //        {
                //            FileName = filePath,
                //            CompId = compId,
                //            ContentType = file.ContentType
                //        });
                //    }
                //}
            }
            catch
            {
               // Console.WriteLine("ReadFormAsync failed: " + ex.Message);
               
            }

            return uploadedFiles;
        }

        public string? GetExtensionFromContentType(string contentType)
        {
            var provider = new FileExtensionContentTypeProvider();
            var mapping = provider.Mappings.FirstOrDefault(m => m.Value.Equals(contentType, StringComparison.OrdinalIgnoreCase));
            return mapping.Key;
        }

        private Task<List<SapDocumentComponent>> ParseSinglepartManuallyAsync(string contentType, Stream body, string docId)
        {
            var uploadedFiles = new List<SapDocumentComponent>();
            try
            {
                var fileName = $"{docId}{GetExtensionFromContentType(contentType)}";
                var filePath = Path.Combine(_saveDirectory, fileName);
                uploadedFiles.Add(new SapDocumentComponent
                {
                    FileName = filePath,
                    Data = body,
                });
            }
            catch
            {
            }

            return Task.FromResult(uploadedFiles);
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
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) &&
                    contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    var fileName = Path.GetFileName(contentDisposition.FileName.Value.Trim('"'));
                    var filePath = Path.Combine(_saveDirectory, fileName);

                    Directory.CreateDirectory(_saveDirectory);

                    if (section.Headers != null)
                    {
                        section.Headers.TryGetValue("X-compId", out var compId);
                        section.Headers.TryGetValue("Content-Type", out var contentTypeHeader);
                        section.Headers.TryGetValue("X-pVersion", out var pVersion);
                        section.Headers.TryGetValue("X-Content-Length", out var contentLength);

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
