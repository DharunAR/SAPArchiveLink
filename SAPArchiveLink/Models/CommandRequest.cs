namespace SAPArchiveLink
{
    public class CommandRequest
    {
        public string Url { get; set; }
        public string HttpMethod { get; set; }
        public string Charset { get; set; }
        public HttpRequest HttpRequest { get; set; }
    }
}
