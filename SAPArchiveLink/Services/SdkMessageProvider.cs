using TRIM.SDK;

namespace SAPArchiveLink
{
    public class SdkMessageProvider : ISdkMessageProvider
    {
        public string GetMessage(MessageIds messageId, string[] args)
        {
            return TrimApplication.GetMessage(messageId, args);
        }
    }
}
