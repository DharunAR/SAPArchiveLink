namespace SAPArchiveLink
{
    public class CommandParameters
    {
        private readonly Dictionary<string, string> _parameters;

        public CommandParameters()
        {
            _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Parse query string into parameters
        public void ParseQueryString(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString)) return;

            queryString = queryString.TrimStart('?');
            var pairs = queryString.Split('&')
                .Select(part => part.Split('=',2))
                .Where(parts => parts.Length == 2);

            foreach (var pair in pairs)
            {
                string key = pair[0];
                string value = Uri.UnescapeDataString(pair[1]);
                _parameters[key] = value;
            }
        }

        // Get a parameter value by key
        public string GetValue(string key)
        {
            return _parameters.TryGetValue(key, out var value) ? value : null;
        }

        // Set a parameter value
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            _parameters[key] = value;
        }
        public string GetStringToSign(bool includeSignature, string charset, string scheme, string hostName, string path)
        {          
            var parametersToSign = _parameters
    .Where(kv => includeSignature || !string.Equals(kv.Key, "secKey", StringComparison.OrdinalIgnoreCase))
    .Select(kv => $"{kv.Key}={kv.Value}");

            string queryString = string.Join("&", parametersToSign);

            if (!string.IsNullOrEmpty(queryString))
                queryString = "?" + queryString;

            return $"{scheme}://{hostName}{path}{queryString}";
        }
    }
}
