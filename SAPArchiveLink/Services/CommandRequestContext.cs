namespace SAPArchiveLink
{
    public class CommandRequestContext : ICommandRequestContext
    {
        private readonly HttpRequest _request;
        private readonly Dictionary<string, string> _lowerCaseHeader;
        private string? _serverName;
        private int _port;
        private long _contentLength;
        private string? _checkSum;
        private Stream? _inputStream;
        private long? _originalLength;

        public CommandRequestContext(HttpRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _lowerCaseHeader = new Dictionary<string, string>();
            _port = -1;
            _contentLength = -1L;
            _originalLength = -1L;

            // Populate headers (convert keys to lowercase, as in ALInput)
            foreach (var header in request.Headers)
            {
                _lowerCaseHeader[header.Key.ToLower()] = header.Value.ToString();
            }
            _lowerCaseHeader["x-ishttps"] = request.IsHttps.ToString().ToLower();
            _lowerCaseHeader["x-pathinfo"] = request.PathBase + (request.Path.HasValue ? request.Path.Value : null);
        }

        public long ConsumePendingClientBody(bool forceIt)
        {
            if (!forceIt && IsExpect100Continue())
            {
                return -1L;
            }

            bool readData = (GetContentLength() > 0 || IsTransferEncoding()) && forceIt;
            if (!readData)
            {
                return -1L;
            }

            long bytesRead = -1L;
            try
            {
                using var stream = GetInputStream();
                byte[] buffer = new byte[4096];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bytesRead += read;
                }
            }
            catch (IOException)
            {
                // Log error if needed
            }
            return bytesRead;
        }

        public string GetCheckSum()
        {
            if (_checkSum == null)
            {
                _checkSum = _lowerCaseHeader.GetValueOrDefault("x-ix-checksum");
                if (_checkSum == null && !string.IsNullOrEmpty(_request.QueryString.Value))
                {
                    var queryString = _request.QueryString.Value.TrimStart('?');
                    const string param = "ixCheckSum=";
                    int idx = queryString.IndexOf(param, StringComparison.Ordinal);
                    if (idx != -1)
                    {
                        _checkSum = queryString.Substring(idx + param.Length);
                        idx = _checkSum.IndexOf('&');
                        if (idx != -1)
                        {
                            _checkSum = _checkSum.Substring(0, idx);
                        }
                        if (!string.IsNullOrEmpty(_checkSum))
                        {
                            // Simulate ALUtils.decodeURLValue (URL decode)
                            _checkSum = Uri.UnescapeDataString(_checkSum);
                        }
                    }
                }
            }
            return _checkSum ?? string.Empty; // Ensure a non-null value is returned
        }

        public HttpRequest GetRequest()
        {
            return _request;
        }

        public long GetContentLength()
        {
            if (_contentLength == -1)
            {
                if (long.TryParse(_lowerCaseHeader.GetValueOrDefault("content-length"), out long length))
                {
                    _contentLength = length;
                }
            }
            return _contentLength;
        }

        public string GetContentType()
        {
            return _lowerCaseHeader.GetValueOrDefault("content-type");
        }

        public string GetHttpMethod()
        {
            return _request.Method;
        }

        public string GetContextPath()
        {
            return _request.PathBase;
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

        public string GetRemoteAddr()
        {
            return _request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        public string GetServerName()
        {
            if (_serverName == null)
            {
                _serverName = _lowerCaseHeader.GetValueOrDefault("host");
                if (!string.IsNullOrEmpty(_serverName))
                {
                    int idx = _serverName.IndexOf(':');
                    if (idx > 0)
                    {
                        if (int.TryParse(_serverName.Substring(idx + 1), out int port))
                        {
                            _port = port;
                        }
                        _serverName = _serverName.Substring(0, idx);
                    }
                }
            }
            return _serverName;
        }

        public int GetPort()
        {
            if (_port == -1)
            {
                GetServerName();
            }
            return _port;
        }

        public string GetPathInfo()
        {
            return _lowerCaseHeader.GetValueOrDefault("x-pathinfo");
        }

        public IReadOnlyDictionary<string, string> GetHeaderMap()
        {
            return _lowerCaseHeader;
        }

        private bool IsExpect100Continue()
        {
            return "100-continue".Equals(_lowerCaseHeader.GetValueOrDefault("expect"), StringComparison.OrdinalIgnoreCase);
        }

        public bool IsHttps()
        {
            return _request.IsHttps;
        }

        private bool IsTransferEncoding()
        {
            string val = _lowerCaseHeader.GetValueOrDefault("transfer-encoding") ?? _lowerCaseHeader.GetValueOrDefault("te");
            return val != null && val.Contains("chunked", StringComparison.OrdinalIgnoreCase);
        }

        public long? GetOriginalLength()
        {
            if (_originalLength == -1L)
            {
                if (long.TryParse(_lowerCaseHeader.GetValueOrDefault("x-original-length"), out long length))
                {
                    _originalLength = length;
                }
                else
                {
                    _originalLength = null;
                }
            }
            return _originalLength;
        }
    }
}
