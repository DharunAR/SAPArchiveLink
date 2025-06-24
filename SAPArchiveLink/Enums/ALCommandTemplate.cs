namespace SAPArchiveLink
{
    public enum ALProtocolVersion
    {
        OO45 = 45,
        OO46 = 46,
        OO47 = 47,
        Unsupported = -1
    }
    public enum ALCommandTemplate
    {
        Unknown,
        // Access Functions
        INFO,
        GET,
        DOCGET,
        CREATEPUT,
        CREATEPOST,
        MCREATE,
        APPEND,
        UPDATE_PUT,
        UPDATE_POST,
        DELETE,
        SEARCH,
        ATTRSEARCH,
        GETANNOTATIONS,
        STOREANNOTATIONS,
        GETNOTES,
        APPENDNOTE,
        GET_HEAD,
        DELATTRIBUTE,
        CSINFO,
        ADMINCONTREP,

        // Admin & Certificate Functions
        PUTCERT,
        SERVERINFO,
        CSRVINFO,
        CSRVSETTINGS,
        GETCERT,
        ADMINFO,
        ANALYZESEC,
        MIGRATE,
        SIGNURL,
        VALIDUSER,
        VERIFYATS,
        VERIFYSIG,
        REINIT,

        // Extra Document Handling
        CREATEPLACEHOLDER,
        RESERVEDOCID,
        GETCONTENT,
        DOCGETFROMCS,
        DISTRIBUTECONTENT,
        FLUSH,
        FREESEARCH,
        GETATS,
        GETATTRIBUTE,
        GETDOCHISTORY,
        LOCK,
        UNLOCK,
        SETATTRIBUTE,
        SETRECORD,
        SETDOCFLAG,
        CACHE
    }

    public enum SslMode
    {
        RequireSsl,
        RequireHttp
    }
}
