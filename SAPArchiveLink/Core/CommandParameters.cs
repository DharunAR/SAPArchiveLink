namespace SAPArchiveLink.Helpers
{
    public class CommandParameters
    {
        private readonly Dictionary<string, string> _params = new Dictionary<string, string>();

        public CommandParameters(string url, string charset)
        {
            // Simulate ALCommand.initFromQueryString logic
            // Parse URL query string (e.g., "contRep=XYZ&docId=123")
            var pairs = url.Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    _params[parts[0]] = parts[1];
                }
            }
        }

        public string GetValue(string name)
        {
            return _params.TryGetValue(name, out var value) ? value : null;
        }
    }
}
