namespace SAPArchiveLink
{
    public class ICSException : Exception
    {
        public string ErrorKey { get; }
        public int HttpStatusCode { get; }

        public ICSException(string errorKey, string message, int httpStatusCode = 500)
            : base(message)
        {
            ErrorKey = errorKey ?? "UNKNOWN";
            HttpStatusCode = httpStatusCode;
        }

        public ICSException(string errorKey, string message, Exception innerException, int httpStatusCode = 500)
            : base(message, innerException)
        {
            ErrorKey = errorKey ?? "UNKNOWN";
            HttpStatusCode = httpStatusCode;
        }
    }

}
