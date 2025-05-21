namespace SAPArchiveLink
{

    /// TODO: These Exception structure is derived from the Java class, 
    /// For .NET we need to work on a better exception mechanism instead of this.
    public class ALException : ICSException
    {

        public const string AL_METHOD_NOT_SUPPORTED = "ICS_5003";
        public const string AL_METHOD_NOT_SUPPORTED_STR = "The ArchiveLink command <{0}> is not supported in this scenario";

        public const string AL_UNKNOWN_HTTP_METHOD = "ICS_5004";
        public const string AL_UNKNOWN_HTTP_METHOD_STR = "The specified HTTP method is not known or not supported in this context: {0} (expected: {1})";

        public const string AL_ERROR_UNKNOWN_COMMAND = "ICS_5005";
        public const string AL_ERROR_UNKNOWN_COMMAND_STR = "The command for the Content Repository is unknown: {0}";

        public const string AL_AMBIGUOUS_QS = "ICS_5006";
        public const string AL_AMBIGUOUS_QS_STR = "The querystring is ambiguous, parameter {0} occurs more than once";

        public const string AL_SHORT_QS = "ICS_5006";
        public const string AL_SHORT_QS_STR = "The query string is not a valid ArchiveLink query string: {0}";

        public const string AL_EMPTY_QS = "ICS_5007";
        public const string AL_EMPTY_QS_STR = "The query string is empty or could not be read";

        public const string AL_UNKNOWN_METHOD_QS = "ICS_5009";
        public const string AL_UNKNOWN_METHOD_QS_STR = "The specified ArchiveLink method is not known or not supported: {0}";

        public const string AL_NOT_ALLOWED = "ICS_5010";
        public const string AL_NOT_ALLOWED_STR = "The request is not allowed: {0}";

        public const string AL_ERROR_MISSING_ATTRIBUTE = "ICS_5011";
        public const string AL_ERROR_MISSING_ATTRIBUTE_STR = "A mandatory attribute was not set: {0}";

        public const string AL_VERSION_NOT_SUPPORTED = "ICS_5012";
        public const string AL_VERSION_NOT_SUPPORTED_STR = "The version {0} is not supported. Supported versions are: {1}";

        public const string AL_INVALID_RANGE = "ICS_5013";
        public const string AL_INVALID_RANGE_STR = "The specified range is invalid: {0}";

        public const string AL_INVALID_VERSION = "ICS_5014";
        public const string AL_INVALID_VERSION_STR = "The specified version is invalid for this command: {0}. At least version {1} must be specified";

        public const string AL_UNEXPECTED_PARAMETER = "ICS_5015";
        public const string AL_UNEXPECTED_PARAMETER_STR = "The parameter {0} is not a valid parameter for the {1} command";

        public const string AL_UNEXPECTED_PARAMVALUE = "ICS_5016";
        public const string AL_UNEXPECTED_PARAMVALUE_STR = "The value {0} is not valid for parameter {1}";

        // Security-related
        public const string AL_AUTHORISATION_REQUIRED = "ICS_5100";
        public static readonly string AL_AUTHORISATION_REQUIRED_STR = "Authorization failed. Please check reason for possible breach of security. URL: {0}";

        public const string AL_NON_SSL_ACCESS = "ICS_5101";
        public const string AL_NON_SSL_ACCESS_STR = "Requests for archive {0} are only allowed with SSL. Clients must use the https protocol.";

        public const string AL_SSL_ACCESS = "ICS_5102";
        public const string AL_SSL_ACCESS_STR = "Requests for archive {0} are not allowed with SSL. Clients must not use the https protocol.";

        public const string AL_SIGNING_ERROR = "ICS_5103";
        public const string AL_SIGNING_ERROR_STR = "Error signing the URL {0}";

        public const string AL_SIGNED_URL_ERROR = "ICS_5104";
        public static readonly string AL_SIGNED_URL_ERROR_STR = "Verification of URL failed. URL: {0}";

        // Wrapper
        public const string AL_ERROR_WRAPPER = "ICS_5002";
        public const string AL_ERROR_WRAPPER_STR = "An error occurred during the execution of the {0} command";

        // Servlet fallback
        public const string SERVLET_DEFAULT_ERROR_MESSAGE = "ICS_5000";
        public const string SERVLET_DEFAULT_ERROR_MESSAGE_STR = "Error in ArchiveLink request";

        public const string SERVLET_CONFIG_INIT = "ICS_5001";
        public const string SERVLET_CONFIG_INIT_STR = "Cannot initialize servlet: {0}";

        public LogLevel LogLevel { get; private set; } = LogLevel.Error;

        public bool IsBodyConsumed { get; private set; }

        public void SetLogInfo() => LogLevel = LogLevel.Information;
        public void SetLogWarn() => LogLevel = LogLevel.Warning;
        public void SetLogError() => LogLevel = LogLevel.Error;
        public void SetBodyConsumed() => IsBodyConsumed = true;

        public ALException(string key, string format, object[] args, int statusCode = 400, Exception? ex = null)
            : base(key, FormatMessage(format, args), ex ?? null, statusCode) { }

        public ALException(string key, string message, int statusCode = 400)
            : base(key, message, statusCode) { }

        public ALException(int statusCode, string message)
       : base(statusCode.ToString(), message, statusCode) { }

        private static string FormatMessage(string format, object[] args)
        {
            try
            {
                return (args == null || args.Length == 0) ? format : string.Format(format, args);
            }
            catch
            {
                return format + " (formatting failed)";
            }
        }
    }

}
