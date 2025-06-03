

namespace SAPArchiveLink
{
    public static class ALCommandTemplateMetadata
    {
        private static readonly Dictionary<ALCommandTemplate, (string AccessMode, string HttpMethod)> _metadata = new()
            {
                { ALCommandTemplate.ADMINFO, (" ", "GET") },
                { ALCommandTemplate.ADMINCONTREP, (" ", "GET") },
                { ALCommandTemplate.ANALYZESEC, (" ", "GET") },
                { ALCommandTemplate.APPEND, ("u", "PUT") },
                { ALCommandTemplate.ATTRSEARCH, ("r", "GET") },
                { ALCommandTemplate.CACHE, ("r", "GET") },
                { ALCommandTemplate.CREATEPLACEHOLDER, ("c", "GET") },
                { ALCommandTemplate.CREATE_PUT, ("c", "PUT") },
                { ALCommandTemplate.CREATE_POST, ("c", "POST") },
                { ALCommandTemplate.CSINFO, ("r", "GET") },
                { ALCommandTemplate.CSRVINFO, (" ", "GET") },
                { ALCommandTemplate.CSRVSETTINGS, (" ", "GET") },
                { ALCommandTemplate.DELATTRIBUTE, ("d", "GET") },
                { ALCommandTemplate.DELETE, ("d", "GET") },
                { ALCommandTemplate.DOCGET, ("r", "GET") },
                { ALCommandTemplate.DOCGETFROMCS, ("r", "GET") },
                { ALCommandTemplate.FLUSH, ("d", "GET") },
                { ALCommandTemplate.FREESEARCH, ("r", "GET") },
                { ALCommandTemplate.GET, ("r", "GET") },
                { ALCommandTemplate.GET_HEAD, ("r", "HEAD") },
                { ALCommandTemplate.GETATTRIBUTE, ("r", "GET") },
                { ALCommandTemplate.GETATS, ("r", "GET") },
                { ALCommandTemplate.GETCERT, (" ", "GET") },
                { ALCommandTemplate.GETDOCHISTORY, ("r", "GET") },
                { ALCommandTemplate.INFO, ("r", "GET") },
                { ALCommandTemplate.LOCK, ("u", "GET") },
                { ALCommandTemplate.MCREATE, ("c", "POST") },
                { ALCommandTemplate.MIGRATE, ("u", "GET") },
                { ALCommandTemplate.PUTCERT, (" ", "PUT") },
                { ALCommandTemplate.REINIT, (" ", "GET") },
                { ALCommandTemplate.RESERVEDOCID, ("c", "GET") },
                { ALCommandTemplate.SEARCH, ("r", "GET") },
                { ALCommandTemplate.SERVERINFO, (" ", "GET") },
                { ALCommandTemplate.SETATTRIBUTE, ("u", "GET") },
                { ALCommandTemplate.SETDOCFLAG, ("c", "GET") },
                { ALCommandTemplate.SETRECORD, ("c", "GET") },
                { ALCommandTemplate.SIGNURL, ("r", "GET") },
                { ALCommandTemplate.UNLOCK, ("u", "GET") },
                { ALCommandTemplate.UPDATE_PUT, ("u", "PUT") },
                { ALCommandTemplate.UPDATE_POST, ("u", "POST") },
                { ALCommandTemplate.VALIDUSER, (" ", "GET") },
                { ALCommandTemplate.VERIFYATS, ("r", "GET") },
                { ALCommandTemplate.VERIFYSIG, ("r", "GET") },
                { ALCommandTemplate.APPENDNOTE, ("u", "PUT") },
                { ALCommandTemplate.GETNOTES, ("r", "GET") },
                { ALCommandTemplate.STOREANNOTATIONS, ("u", "PUT") },
                { ALCommandTemplate.GETANNOTATIONS, ("r", "GET") },
                { ALCommandTemplate.DISTRIBUTECONTENT, ("c", "PUT") },
                { ALCommandTemplate.GETCONTENT, ("r", "GET") }
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
