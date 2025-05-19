namespace SAPArchiveLink
{
    public class ALException : Exception
    {
        public int? StatusCode { get; }

        public ALException(int? statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
