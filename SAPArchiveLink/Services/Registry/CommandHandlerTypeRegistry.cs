namespace SAPArchiveLink
{

    /// <summary>
    /// Registers all command handler types for SAP ArchiveLink commands.
    /// </summary>
    public static class CommandHandlerTypeRegistry
    {
        private static readonly Dictionary<ALCommandTemplate, Type> _handlerTypes = new()
    {
         { ALCommandTemplate.ADMINFO, typeof(AdmInfoCommandHandler) },
    { ALCommandTemplate.ADMINCONTREP, typeof(AdminContRepCommandHandler) },
    { ALCommandTemplate.ANALYZESEC, typeof(AnalyzeSecCommandHandler) },
    { ALCommandTemplate.APPEND, typeof(AppendCommandHandler) },
    { ALCommandTemplate.ATTRSEARCH, typeof(AttrSearchCommandHandler) },
    { ALCommandTemplate.CACHE, typeof(CacheCommandHandler) },
    { ALCommandTemplate.CREATEPLACEHOLDER, typeof(CreatePlaceholderCommandHandler) },
    { ALCommandTemplate.CREATEPUT, typeof(CreatePutCommandHandler) },
    { ALCommandTemplate.CREATEPOST, typeof(CreatePostCommandHandler) },
    { ALCommandTemplate.CSINFO, typeof(CsInfoCommandHandler) },
    { ALCommandTemplate.CSRVINFO, typeof(CsrvInfoCommandHandler) },
    { ALCommandTemplate.CSRVSETTINGS, typeof(CsrvSettingsCommandHandler) },
    { ALCommandTemplate.DELATTRIBUTE, typeof(DelAttributeCommandHandler) },
    { ALCommandTemplate.DELETE, typeof(DeleteCommandHandler) },
    { ALCommandTemplate.DOCGET, typeof(DocGetCommandHandler) },
    { ALCommandTemplate.DOCGETFROMCS, typeof(DocGetFromCsCommandHandler) },
    { ALCommandTemplate.FLUSH, typeof(FlushCommandHandler) },
    { ALCommandTemplate.FREESEARCH, typeof(FreeSearchCommandHandler) },
    { ALCommandTemplate.GET, typeof(GetCommandHandler) },
    { ALCommandTemplate.GET_HEAD, typeof(GetHeadCommandHandler) },
    { ALCommandTemplate.GETATTRIBUTE, typeof(GetAttributeCommandHandler) },
    { ALCommandTemplate.GETATS, typeof(GetAtsCommandHandler) },
    { ALCommandTemplate.GETCERT, typeof(GetCertCommandHandler) },
    { ALCommandTemplate.GETDOCHISTORY, typeof(GetDocHistoryCommandHandler) },
    { ALCommandTemplate.INFO, typeof(InfoCommandHandler) },
    { ALCommandTemplate.LOCK, typeof(LockCommandHandler) },
    { ALCommandTemplate.MCREATE, typeof(MCreateCommandHandler) },
    { ALCommandTemplate.MIGRATE, typeof(MigrateCommandHandler) },
    { ALCommandTemplate.PUTCERT, typeof(PutCertCommandHandler) },
    { ALCommandTemplate.REINIT, typeof(ReinitCommandHandler) },
    { ALCommandTemplate.RESERVEDOCID, typeof(ReserveDocIdCommandHandler) },
    { ALCommandTemplate.SEARCH, typeof(SearchCommandHandler) },
    { ALCommandTemplate.SERVERINFO, typeof(ServerInfoCommandHandler) },
    { ALCommandTemplate.SETATTRIBUTE, typeof(SetAttributeCommandHandler) },
    { ALCommandTemplate.SETDOCFLAG, typeof(SetDocFlagCommandHandler) },
    { ALCommandTemplate.SETRECORD, typeof(SetRecordCommandHandler) },
    { ALCommandTemplate.SIGNURL, typeof(SignUrlCommandHandler) },
    { ALCommandTemplate.UNLOCK, typeof(UnlockCommandHandler) },
    { ALCommandTemplate.UPDATE_PUT, typeof(UpdateCommandHandler) },
    { ALCommandTemplate.UPDATE_POST, typeof(UpdatePostCommandHandler) },
    { ALCommandTemplate.VALIDUSER, typeof(ValidUserCommandHandler) },
    { ALCommandTemplate.VERIFYATS, typeof(VerifyAtsCommandHandler) },
    { ALCommandTemplate.VERIFYSIG, typeof(VerifySigCommandHandler) },
    { ALCommandTemplate.STOREANNOTATIONS, typeof(StoreAnnotationsCommandHandler) },
    { ALCommandTemplate.GETANNOTATIONS, typeof(GetAnnotationsCommandHandler) },
    { ALCommandTemplate.DISTRIBUTECONTENT, typeof(DistributeContentCommandHandler) },
    { ALCommandTemplate.GETCONTENT, typeof(GetContentCommandHandler) }
    };

        public static Type? GetHandlerType(ALCommandTemplate template) => _handlerTypes.TryGetValue(template, out var type) ? type : null;
    }
}
