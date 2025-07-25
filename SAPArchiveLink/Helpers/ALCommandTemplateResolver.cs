namespace SAPArchiveLink
{
    public static class ALCommandTemplateResolver
    {
        public static ALCommandTemplate Parse(string httpMethod, string url)
        {
            // Simplified URL parsing: extract the command name (first parameter)
            var commandName = url.Split('&')[0].Split('=')[0].ToLower();

            return (httpMethod.ToUpper(), commandName) switch
            {
                ("PUT", "append") => ALCommandTemplate.APPEND,
                ("GET", "attrsearch") => ALCommandTemplate.ATTRSEARCH,
                ("PUT", "create") => ALCommandTemplate.CREATEPUT,
                ("POST", "create") => ALCommandTemplate.CREATEPOST,
                ("GET", "delete") => ALCommandTemplate.DELETE,
                ("GET", "docget") => ALCommandTemplate.DOCGET,
                ("GET", "get") => ALCommandTemplate.GET,
                ("GET", "info") => ALCommandTemplate.INFO,
                ("POST", "mcreate") => ALCommandTemplate.MCREATE,
                ("PUT", "putcert") => ALCommandTemplate.PUTCERT,
                ("GET", "search") => ALCommandTemplate.SEARCH,
                ("GET", "serverinfo") => ALCommandTemplate.SERVERINFO,
                ("PUT", "update") => ALCommandTemplate.UPDATE_PUT,
                ("POST", "update") => ALCommandTemplate.UPDATE_POST,
                _ => ALCommandTemplate.Unknown
            };
        }
    }
}
