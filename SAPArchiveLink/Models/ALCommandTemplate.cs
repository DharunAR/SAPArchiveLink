namespace SAPArchiveLink.Helpers
{
    public enum ALCommandTemplate
    {
        // Access Functions
        INFO,
        GET,
        DOCGET,
        CREATE_PUT,
        CREATE_POST,
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
}
