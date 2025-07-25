namespace SAPArchiveLink
{
    public interface ICommandRequestContext
    {
        HttpRequest GetRequest();
        string GetHttpMethod();
        Stream GetInputStream();
        string GetALQueryString(bool useContextPathIfNull);
    }
}
