using Microsoft.AspNetCore.StaticFiles;
using System.Net.Mime;

namespace SAPArchiveLink
{
    public class CommandResponse: ICommandResponse
    {
        public Stream StreamContent { get; private set; }
        public string TextContent { get; private set; }
        public bool IsStream { get; private set; }
        public List<SapDocumentComponentModel> Components { get; private set; } = new();
        public string Boundary { get; private set; }

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string ContentType { get; set; } = MediaTypeNames.Text.Plain + "; charset=UTF-8";
        public Dictionary<string, string> Headers { get; set; } = new();

        private CommandResponse() { }

        /// <summary>
        /// Creates a plain text ArchiveLink response with specified content and status code.
        /// Used for standard SAP protocol responses.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="statusCode"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static CommandResponse ForProtocolText(string content = "", int statusCode = StatusCodes.Status200OK, string charset = "UTF-8")
        {
            return new CommandResponse
            {
                TextContent = content,
                StatusCode = statusCode,
                ContentType = $"{MediaTypeNames.Text.Plain}; charset={charset}",
                IsStream = false
            };
        }

        /// <summary>
        /// Creates an HTML-formatted response, typically used for administrative commands (e.g., AdmInfo).
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <param name="statusCode"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static CommandResponse ForHtmlReport(string htmlContent, int statusCode = StatusCodes.Status200OK, string charset = "UTF-8")
        {
            return new CommandResponse
            {
                TextContent = htmlContent,
                StatusCode = statusCode,
                ContentType = $"{MediaTypeNames.Text.Html}; charset={charset}",
                IsStream = false
            };
        }

        /// <summary>
        /// Creates a streamed response for a single binary document component. 
        /// </summary>
        /// <param name="contentStream"></param>
        /// <param name="contentType"></param>
        /// <param name="statusCode"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static CommandResponse ForDocumentContent(Stream contentStream, string contentType = MediaTypeNames.Application.Octet, int statusCode = StatusCodes.Status200OK, string fileName = null)
        {
            var response = new CommandResponse
            {
                StreamContent = contentStream,
                StatusCode = statusCode,
                ContentType = GetMimeTypeFromExtension(fileName),
                IsStream = true
            };

            return response;
        }

        /// <summary>
        /// Creates a multipart/form-data response containing multiple document components.
        /// </summary>
        /// <param name="components"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static CommandResponse ForMultipartDocument(List<SapDocumentComponentModel> components, int statusCode = StatusCodes.Status200OK)
        {
            string boundary = $"docGet_{Guid.NewGuid():N}";
            var response =  new CommandResponse
            {
                Components = components,
                StatusCode = statusCode,
                ContentType = $"multipart/form-data; boundary={boundary}",
                Boundary = boundary,
                IsStream = true
            };
            return response;
        }

        /// <summary>
        /// Creates a plain text error response with an SAP ArchiveLink-compliant message body
        /// and appropriate HTTP status code.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static CommandResponse ForError(string message, int statusCode = StatusCodes.Status400BadRequest)
        {
            var response = new CommandResponse
            {
                TextContent = $"ErrorMessage={message}",
                StatusCode = statusCode,
                ContentType = $"{MediaTypeNames.Text.Plain}; charset=UTF-8",
                IsStream = false
            };

            response.AddHeader("X-ErrorDescription", message);
            return response;
        }


        /// <summary>
        /// Add a custom response header
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddHeader(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null)
            {
                Headers[key] = value;
            }
        }

        /// <summary>
        /// Get the MIME type based on the file extension
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetMimeTypeFromExtension(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out string contentType))
            {
                contentType = MediaTypeNames.Application.Octet;
            }
            return contentType;
        }

    }
}
