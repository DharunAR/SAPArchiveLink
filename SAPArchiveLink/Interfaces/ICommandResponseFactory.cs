namespace SAPArchiveLink
{
    public interface ICommandResponseFactory
    {
        ICommandResponse CreateProtocolText(string content, int statusCode = 200, string charset = "UTF-8");
        ICommandResponse CreateHtmlReport(string htmlContent, int statusCode = 200, string charset = "UTF-8");
        ICommandResponse CreateDocumentContent(Stream contentStream, string contentType = "application/octet-stream", int statusCode = 200, string fileName = null);
        ICommandResponse CreateMultipartDocument(List<SAPDocumentComponent> components, int statusCode = 200);
        ICommandResponse CreateError(string message, int statusCode = StatusCodes.Status400BadRequest);
    }
}
