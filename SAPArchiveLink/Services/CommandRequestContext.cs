namespace SAPArchiveLink
{
    public class CommandRequestContext : ICommandRequestContext
    {
        private readonly HttpRequest _request;
        private Stream? _inputStream;

        public CommandRequestContext(HttpRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public HttpRequest GetRequest()
        {
            return _request;
        }

        public string GetHttpMethod()
        {
            return _request.Method;
        }

        public Stream GetInputStream()
        {
            if (_inputStream == null)
            {
                _inputStream = _request.Body;
            }
            return _inputStream;
        }

        public string GetALQueryString(bool useContextPathIfNull)
        {
            string queryString = GetQueryString(useContextPathIfNull);
            if (string.IsNullOrEmpty(queryString))
            {
                return queryString;
            }

            const string pattern = "&forward=";
            int pIndex = queryString.IndexOf(pattern, StringComparison.Ordinal);
            if (pIndex == -1)
            {
                return queryString;
            }

            int nIndex = queryString.IndexOf('&', pIndex + pattern.Length);
            if (nIndex == -1)
            {
                throw new ArgumentException($"Cannot find valid queryString in forward URL: {queryString}");
            }

            string command = queryString.Substring(0, pIndex);
            string paramsPart = queryString.Substring(nIndex);
            return command + paramsPart;
        }

        private string GetQueryString(bool useContextPathIfNull)
        {
            string queryString = _request.QueryString.HasValue ? _request.QueryString.Value.TrimStart('?') : null;
            return string.IsNullOrEmpty(queryString) && useContextPathIfNull ? _request.PathBase : queryString;
        }
    }
}
