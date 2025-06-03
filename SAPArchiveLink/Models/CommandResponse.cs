using System.Net.Mime;

namespace SAPArchiveLink
{
    public class CommandResponse: ICommandResponse
    {
        public Stream StreamContent { get; private set; }
        public string TextContent { get; private set; }
        public bool IsStream { get; private set; }
        public List<SAPDocumentComponent> Components { get; private set; } = new();
        public string Boundary { get; private set; }

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string ContentType { get; set; } = MediaTypeNames.Text.Plain + "; charset=UTF-8";
        public Dictionary<string, string> Headers { get; set; } = new();

        private CommandResponse() { }

        public static CommandResponse ForProtocolText(string content, int statusCode = StatusCodes.Status200OK, string charset = "UTF-8")
        {
            return new CommandResponse
            {
                TextContent = content,
                StatusCode = statusCode,
                ContentType = $"{MediaTypeNames.Text.Plain}; charset={charset}",
                IsStream = false
            };
        }

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

        public static CommandResponse ForDocumentContent(Stream contentStream, string contentType = MediaTypeNames.Application.Octet, int statusCode = StatusCodes.Status200OK, string fileName = null)
        {
            var response = new CommandResponse
            {
                StreamContent = contentStream,
                StatusCode = statusCode,
                ContentType = contentType,
                IsStream = true
            };

            if (!string.IsNullOrEmpty(fileName))
            {
                response.AddHeader("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            }

            return response;
        }

        public static CommandResponse ForMultipartDocument(List<SAPDocumentComponent> components, int statusCode = StatusCodes.Status200OK)
        {
            string boundary = $"docGet_{Guid.NewGuid():N}";
            return new CommandResponse
            {
                Components = components,
                StatusCode = statusCode,
                ContentType = $"multipart/form-data; boundary={boundary}",
                Boundary = boundary,
                IsStream = true
            };
        }

        public static CommandResponse ForError(string message, string errorCode = "ICS_5000", int statusCode = StatusCodes.Status400BadRequest)
        {
            return ForProtocolText($"ErrorCode={errorCode}\nErrorMessage={message}", statusCode);
        }

        public void AddHeader(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null)
            {
                Headers[key] = value;
            }
        }
    }
}
