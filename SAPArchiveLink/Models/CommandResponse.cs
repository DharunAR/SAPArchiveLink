using System.Net.Mime;

namespace SAPArchiveLink
{
    public class CommandResponse
    {
        public Stream StreamContent { get; private set; }
        public string TextContent { get; private set; }
        public bool IsStream { get; private set; }

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string ContentType { get; set; } = MediaTypeNames.Text.Plain + "; charset=UTF-8";
        public Dictionary<string, string> Headers { get; set; } = new();

        private CommandResponse() { }

        // For standard plain text SAP ArchiveLink protocol responses
        public static CommandResponse ForProtocolText(string content, int statusCode = StatusCodes.Status200OK, string charset = "UTF-8")
        {
            return new CommandResponse
            {
                TextContent = content,
                StatusCode = statusCode,
                ContentType = MediaTypeNames.Text.Plain + "; charset=" + charset,
                IsStream = false
            };
        }

        // For HTML formatted responses, typically administrative info
        public static CommandResponse ForHtmlReport(string htmlContent, int statusCode = StatusCodes.Status200OK, string charset = "UTF-8")
        {
            return new CommandResponse
            {
                TextContent = htmlContent,
                StatusCode = statusCode,
                ContentType = MediaTypeNames.Text.Html + "; charset=" + charset,
                IsStream = false
            };
        }

        // For actual binary document content retrieval (e.g., GET_CONTENT)
        public static CommandResponse ForDocumentContent(Stream contentStream, string contentType = MediaTypeNames.Application.Octet, int statusCode = StatusCodes.Status200OK)
        {
            return new CommandResponse
            {
                StreamContent = contentStream,
                StatusCode = statusCode,
                ContentType = contentType,
                IsStream = true
            };
        }

        // Error response (can be kept as is, or use ForProtocolText with error formatting)
        public static CommandResponse ForError(string message, string errorCode = "ICS_5000", int statusCode = StatusCodes.Status400BadRequest)
        {
            return ForProtocolText($"ErrorCode={errorCode}\nErrorMessage={message}", statusCode);
        }

        // Helper to add headers
        public void AddHeader(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                Headers[key] = value;
            }
        }
    }
}
