

namespace SAPArchiveLink
{
    public static class ALCommandTemplateMetadata
    {
        private static readonly Dictionary<ALCommandTemplate, (string AccessMode, string HttpMethod)> _metadata = new()
            {
                { ALCommandTemplate.APPEND, ("u", "PUT") },
                { ALCommandTemplate.ATTRSEARCH, ("r", "GET") },
                { ALCommandTemplate.CREATEPUT, ("c", "PUT") },
                { ALCommandTemplate.CREATEPOST, ("c", "POST") },
                { ALCommandTemplate.DELETE, ("d", "GET") },
                { ALCommandTemplate.DOCGET, ("r", "GET") },
                { ALCommandTemplate.GET, ("r", "GET") },
                { ALCommandTemplate.INFO, ("r", "GET") },
                { ALCommandTemplate.MCREATE, ("c", "POST") },
                { ALCommandTemplate.PUTCERT, (" ", "PUT") },
                { ALCommandTemplate.SEARCH, ("r", "GET") },
                { ALCommandTemplate.SERVERINFO, (" ", "GET") },
                { ALCommandTemplate.UPDATE_PUT, ("u", "PUT") },
                { ALCommandTemplate.UPDATE_POST, ("u", "POST") }
            };

        public static string GetAccessMode(ALCommandTemplate template)
        {
            return _metadata.TryGetValue(template, out var meta) ? meta.AccessMode : "";
        }

        public static string GetHttpMethod(ALCommandTemplate template)
        {
            return _metadata.TryGetValue(template, out var meta) ? meta.HttpMethod : "GET";
        }
    }
}
