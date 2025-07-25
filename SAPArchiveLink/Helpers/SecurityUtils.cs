namespace SAPArchiveLink
{
    public static class SecurityUtils
    {
        const string Attachment = "attachment";
        const string Inline = "inline";

        private static readonly HashSet<string> SafeMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
          "application/pdf",
          "image/jpeg",
          "image/png",
          "image/gif",
          "image/webp",
          "application/msword",
          "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private static readonly HashSet<string> UnsafeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
          ".html", ".htm", ".svg", ".xml", ".js", ".json", ".xhtml", ".jsp", ".php", ".mhtml"
        };

        /// <summary>
        /// Determines if a file is safe to be displayed inline in a browser based on its content type and file extension.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="extension"></param>
        /// <returns></returns>

        public static bool IsSafeForInline(string contentType, string extension)
        {
            if (!string.IsNullOrEmpty(extension) && UnsafeExtensions.Contains(extension.ToLowerInvariant()))
                return false;

            if (string.IsNullOrWhiteSpace(contentType))
                return false;

            return SafeMimeTypes.Contains(contentType);
        }

        /// <summary>
        /// Converts a protection level string (e.g., "rcud") to an integer representation. 
        /// </summary>
        /// <param name="protLevelStr"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static int AccessModeToInt(string protLevelStr)
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
                    string msg = $"Protection Level {protLevelStr} is invalid. Only (rcud) is allowed.";
                    Console.Error.WriteLine($"{methodName}: {msg}");
                    throw new InvalidOperationException(msg);
                }
            }

            return protectionLevel;
        }

        /// <summary>
        /// Generates a Content-Disposition header value based on the file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetContentDispositionValue(string fileName, string mimeType, out bool addNoSniffHeader)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name must not be null or empty.", nameof(fileName));

            var extension = Path.GetExtension(fileName);
            var dispositionType = IsSafeForInline(mimeType, extension) ? Inline : Attachment;

            addNoSniffHeader = (dispositionType == Attachment);

            var sanitizedFileName = Path.GetFileName(fileName).Replace("\"", "");
            return $"{dispositionType}; filename=\"{sanitizedFileName}\"";
        }

        /// <summary>
        /// Parses a version string into an ALProtocolVersion enum.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static ALProtocolVersion ParseVersion(string version) => version switch
        {
            "0045" => ALProtocolVersion.OO45,
            "0046" => ALProtocolVersion.OO46,
            "0047" => ALProtocolVersion.OO47,
            _ => ALProtocolVersion.Unsupported
        };
    }

}
