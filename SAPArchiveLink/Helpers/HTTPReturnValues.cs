namespace SAPArchiveLink
{
    public class HTTPReturnValues
    {
        // Definitions of HTTP return values

        public const int HTTP_OK = 200;
        public const int HTTP_CREATED = 201;
        public const int HTTP_PART = 206;
        public const int HTTP_250 = 250;
        public const int HTTP_MOVED_PERMANENTLY = 301;
        public const int HTTP_MOVED = 302;
        public const int HTTP_SEE_OTHER = 303;
        public const int HTTP_NOT_MODIFIED = 304;
        public const int HTTP_USE_PROXY = 305;
        public const int HTTP_TEMPORARY_REDIRECT = 307;
        public const int HTTP_BADREQUEST = 400;
        public const int HTTP_BREACHOFSECURITY = 401;
        public const int HTTP_FORBIDDEN = 403;
        public const int HTTP_NOTFOUND = 404;
        public const int HTTP_CONFLICT = 409;
        public const int HTTP_PRECONDITION_FAILED = 412;
        public const int HTTP_INTERNAL_SERVER_ERROR = 500;
        public const int HTTP_NOT_IMPLEMENTED = 501;
        public const int HTTP_SERVICE_UNAVAILABLE = 503;
        public const int HTTP_ON_TAPE = 560; // Means a delayed response
        public const int HTTP_IO_ERROR_AFTER_RESPONSE = 561;
    }
}
