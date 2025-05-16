namespace SAPArchiveLink.Helpers
{
    public class CommandResponse
    {
        public string Content { get; set; }
        public int StatusCode { get; set; } = 200;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public CommandResponse(string content)
        {
            Content = content;
        }
    }
}
