using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface ISdkMessageProvider
    {
        string GetMessage(MessageIds messageId, string[] args);
    }
}
