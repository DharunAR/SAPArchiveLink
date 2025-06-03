namespace SAPArchiveLink
{
    public class CommandResponseFactory : ICommandResponseFactory
    {
        public ICommandResponse CreateProtocolText(string content, int statusCode = 200, string charset = "UTF-8")
            => CommandResponse.ForProtocolText(content, statusCode, charset);

        public ICommandResponse CreateHtmlReport(string htmlContent, int statusCode = 200, string charset = "UTF-8")
            => CommandResponse.ForHtmlReport(htmlContent, statusCode, charset);

        public ICommandResponse CreateDocumentContent(Stream contentStream, string contentType = "application/octet-stream", int statusCode = 200, string fileName = null)
            => CommandResponse.ForDocumentContent(contentStream, contentType, statusCode, fileName);

        public ICommandResponse CreateMultipartDocument(List<SAPDocumentComponent> components, int statusCode = 200)
            => CommandResponse.ForMultipartDocument(components, statusCode);

        public ICommandResponse CreateError(string message, string errorCode = "ICS_5000", int statusCode = 400)
            => CommandResponse.ForError(message, errorCode, statusCode);
    }

}
