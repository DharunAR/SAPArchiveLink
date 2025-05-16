using SAPArchiveLink.Models;

namespace SAPArchiveLink.Helpers
{
    public static class ALCommandTemplateResolver
    {
        public static ALCommandTemplate Parse(string httpMethod, string url)
        {
            // Simplified URL parsing: extract the command name (first parameter)
            var commandName = url.Split('&')[0].Split('=')[0].ToLower();

            return (httpMethod.ToUpper(), commandName) switch
            {
                ("GET", "adminfo") => ALCommandTemplate.ADMINFO,
                ("GET", "admincontrep") => ALCommandTemplate.ADMINCONTREP,
                ("GET", "analyzesec") => ALCommandTemplate.ANALYZESEC,
                ("PUT", "append") => ALCommandTemplate.APPEND,
                ("GET", "attrsearch") => ALCommandTemplate.ATTRSEARCH,
                ("GET", "cache") => ALCommandTemplate.CACHE,
                ("GET", "createplaceholder") => ALCommandTemplate.CREATEPLACEHOLDER,
                ("PUT", "create") => ALCommandTemplate.CREATE_PUT,
                ("POST", "create") => ALCommandTemplate.CREATE_POST,
                ("GET", "csinfo") => ALCommandTemplate.CSINFO,
                ("GET", "csrvinfo") => ALCommandTemplate.CSRVINFO,
                ("GET", "csrvsettings") => ALCommandTemplate.CSRVSETTINGS,
                ("GET", "delattribute") => ALCommandTemplate.DELATTRIBUTE,
                ("GET", "delete") => ALCommandTemplate.DELETE,
                ("GET", "docget") => ALCommandTemplate.DOCGET,
                ("GET", "docgetfromcs") => ALCommandTemplate.DOCGETFROMCS,
                ("GET", "flush") => ALCommandTemplate.FLUSH,
                ("GET", "freesearch") => ALCommandTemplate.FREESEARCH,
                ("GET", "get") => ALCommandTemplate.GET,
                ("HEAD", "get") => ALCommandTemplate.GET_HEAD,
                ("GET", "getattribute") => ALCommandTemplate.GETATTRIBUTE,
                ("GET", "getats") => ALCommandTemplate.GETATS,
                ("GET", "getcert") => ALCommandTemplate.GETCERT,
                ("GET", "getdocumenthistory") => ALCommandTemplate.GETDOCHISTORY,
                ("GET", "info") => ALCommandTemplate.INFO,
                ("GET", "lock") => ALCommandTemplate.LOCK,
                ("POST", "mcreate") => ALCommandTemplate.MCREATE,
                ("GET", "migrate") => ALCommandTemplate.MIGRATE,
                ("PUT", "putcert") => ALCommandTemplate.PUTCERT,
                ("GET", "reinit") => ALCommandTemplate.REINIT,
                ("GET", "reservedocid") => ALCommandTemplate.RESERVEDOCID,
                ("GET", "search") => ALCommandTemplate.SEARCH,
                ("GET", "serverinfo") => ALCommandTemplate.SERVERINFO,
                ("GET", "setattribute") => ALCommandTemplate.SETATTRIBUTE,
                ("GET", "setdocumentflag") => ALCommandTemplate.SETDOCFLAG,
                ("GET", "setrecord") => ALCommandTemplate.SETRECORD,
                ("GET", "signurl") => ALCommandTemplate.SIGNURL,
                ("GET", "unlock") => ALCommandTemplate.UNLOCK,
                ("PUT", "update") => ALCommandTemplate.UPDATE_PUT,
                ("POST", "update") => ALCommandTemplate.UPDATE_POST,
                ("GET", "validuser") => ALCommandTemplate.VALIDUSER,
                ("GET", "verifyats") => ALCommandTemplate.VERIFYATS,
                ("GET", "verifysig") => ALCommandTemplate.VERIFYSIG,
                ("PUT", "appendnote") => ALCommandTemplate.APPENDNOTE,
                ("GET", "getnotes") => ALCommandTemplate.GETNOTES,
                ("PUT", "storeannotations") => ALCommandTemplate.STOREANNOTATIONS,
                ("GET", "getannotations") => ALCommandTemplate.GETANNOTATIONS,
                ("PUT", "distributecontent") => ALCommandTemplate.DISTRIBUTECONTENT,
                ("GET", "getcontent") => ALCommandTemplate.GETCONTENT,
                _ => throw new ALException(400, $"Unknown command: {commandName} for HTTP method {httpMethod}")
            };
        }
    }
}
