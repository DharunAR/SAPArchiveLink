namespace SAPArchiveLink
{
    public interface ICommandRequestContext
    {
        string GetALQueryString(bool removeSignature);
        string GetHttpMethod();
        string GetServerName();
        int GetPort();
        string GetContextPath();
        public long ConsumePendingClientBody(bool forceIt);
        public string GetCheckSum();
        public HttpRequest GetRequest();
    }
}
