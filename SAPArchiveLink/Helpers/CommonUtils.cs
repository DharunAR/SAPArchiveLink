using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Reflection;

namespace SAPArchiveLink
{
    public class CommonUtils : HTTPReturnValues
    {
        public static void CloseStream(Stream stream, ILogger logger)
        {
            const string MN = "CloseStream: ";
            if (stream != null)
            {
                try
                {
                    stream.Close();
                }
                catch (IOException ex)
                {
                    logger.LogDebug(ex, $"{MN} Closing stream failed");
                    logger.LogWarning($"{MN} Closing stream failed: {ex.Message}");
                }
            }
        }
        public static int ConvertProtection(string protLevelStr)
        {
            const string methodName = "ConvertProtection";
            int protectionLevel = 0;

            if (!string.IsNullOrEmpty(protLevelStr))
            {
                bool isError = false;

                foreach (char cur in protLevelStr)
                {
                    switch (cur)
                    {
                        case ALCommand.PROT_READ:
                            protectionLevel |= ALCommand.PROT_NO_READ;
                            break;
                        case ALCommand.PROT_CREATE:
                            protectionLevel |= ALCommand.PROT_NO_CREATE;
                            break;
                        case ALCommand.PROT_UPDATE:
                            protectionLevel |= ALCommand.PROT_NO_UPDATE;
                            break;
                        case ALCommand.PROT_DELETE:
                            protectionLevel |= ALCommand.PROT_NO_DELETE;
                            break;
                        case ALCommand.PROT_ELIB:
                            protectionLevel |= ALCommand.PROT_NO_ELIB;
                            break;
                        default:
                            isError = true;
                            break;
                    }
                }

                if (isError)
                {
                    string msg = $"Protection Level \"{protLevelStr}\" is invalid. Only (rcud) is allowed.";
                   // LogHelper.Error(methodName, msg, new InvalidOperationException(msg));
                    throw new InvalidOperationException(msg);
                }
            }

            return protectionLevel;
        }
    }
}
