namespace SAPArchiveLink
{
    public interface ICommandResponseFactory
    {
        ICommandResponse CreateProtocolText(string content, int statusCode = StatusCodes.Status200OK, string charset = "UTF-8");
        ICommandResponse CreateHtmlReport(string htmlContent, int statusCode = StatusCodes.Status200OK, string charset = "UTF-8");
        ICommandResponse CreateDocumentContent(Stream contentStream, string contentType = "application/octet-stream", int statusCode = StatusCodes.Status200OK, string fileName = null);
        ICommandResponse CreateMultipartDocument(List<SapDocumentComponentModel> components, int statusCode = 200);
        ICommandResponse CreateError(string message, int statusCode = StatusCodes.Status400BadRequest);
        ICommandResponse CreateInfoMetadata(List<SapDocumentComponentModel> components, int statusCode = StatusCodes.Status200OK);
    }
}
