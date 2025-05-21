namespace SAPArchiveLink
{
    public class CommandResponse
    {
        public byte[] BinaryContent { get; private set; }
        public string TextContent { get; private set; }
        public bool IsBinary { get; private set; }

        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "text/plain; charset=UTF-8";
        public Dictionary<string, string> Headers { get; set; } = new();

        private CommandResponse() { }

        // Text response (e.g., info, errors, metadata)
        public static CommandResponse FromText(string content, int statusCode = 200, string contentType = "text/plain; charset=UTF-8")
        {
            return new CommandResponse
            {
                TextContent = content,
                StatusCode = statusCode,
                ContentType = contentType,
                IsBinary = false
            };
        }

        // Binary response (e.g., docGet)
        public static CommandResponse FromBinary(byte[] content, string contentType = "application/octet-stream", int statusCode = 200)
        {
            var response = new CommandResponse
            {
                BinaryContent = content,
                StatusCode = statusCode,
                ContentType = contentType,
                IsBinary = true
            };
            if (content != null)
            {
                response.Headers["Content-Length"] = content.Length.ToString();
            }
            return response;
        }

        // Error response
        // Can optionally add contentType parameter if different error formats are needed
        public static CommandResponse FromError(string message, string errorCode = "ICS_5000", int statusCode = 400)
        {
            return FromText($"ErrorCode={errorCode}\nErrorMessage={message}", statusCode);
        }
    }
}
