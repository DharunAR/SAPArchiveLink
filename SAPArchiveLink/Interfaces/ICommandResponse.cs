namespace SAPArchiveLink
{
    public interface ICommandResponse
    {
        Stream StreamContent { get; }
        string TextContent { get; }
        bool IsStream { get; }
        List<SapDocumentComponentModel> Components { get; }
        string Boundary { get; }
        int StatusCode { get; set; }
        string ContentType { get; set; }
        Dictionary<string, string> Headers { get; }
        void AddHeader(string key, string value);
    }
}
