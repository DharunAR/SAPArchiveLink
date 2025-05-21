namespace SAPArchiveLink
{
    public interface ICommandRequestContext
    {
        long ConsumePendingClientBody(bool forceIt);
        string GetCheckSum();
        HttpRequest GetRequest();
        long GetContentLength();
        string GetContentType();
        string GetHttpMethod();
        string GetContextPath();
        Stream GetInputStream();
        string GetALQueryString(bool useContextPathIfNull);
        string GetRemoteAddr();
        string GetServerName();
        int GetPort();
        string GetPathInfo();
        IReadOnlyDictionary<string, string> GetHeaderMap();
        bool IsHttps();
        long? GetOriginalLength();
    }
}
